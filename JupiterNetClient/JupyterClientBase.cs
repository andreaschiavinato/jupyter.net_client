using JupiterNetClient.Nbformat;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using ZeroMQ;

namespace JupiterNetClient
{
    public abstract class JupyterClientBase
    {
        #region Events
        public event EventHandler OnConnected;
        public event EventHandler OnShutdown;
        public event EventHandler<KernelState> OnStatus;
        public event EventHandler<JupyterMessage> OnOutputMessage;
        public event EventHandler<(string prompt, bool password)> OnInputRequest;
        #endregion

        #region Public properties
        public KernelSpec KernelSpec { get; private set; }
        public JupyterMessage.KernelInfoReplyContent KernelInfo { get; private set; }
        #endregion

        #region Private vars
        protected ConnectionInfo ConnectionInfo;        
        protected ZSocket ShellSocket;
        protected ZSocket IoPubSocket;
        protected ZSocket StdInSocket;
        protected ZSocket ControlSocket;
        protected KernelState CurrentState;
        protected ManualResetEventSlim ContinueShutdown;
        #endregion

        #region Private vars
        private Session _session;
        private ZContext _context;
        private KernelManager _kernelManager;
        #endregion

        public JupyterClientBase(string pythonFolder = "")
        {
            _session = new Session();
            _kernelManager = new KernelManager();
            _kernelManager.Initialize(pythonFolder);
        }

        public Dictionary<string, KernelSpec> GetKernels() =>
            _kernelManager.KernelSpecs.kernelspecs;

        public void StartKernel(string kernelName)
        {
            var connectionFile = _kernelManager.StartKernel(_session.Id, kernelName);
            Connect(connectionFile);
           KernelSpec = _kernelManager.KernelSpecs.kernelspecs[kernelName];
        }

        public void Connect(string connectionFile)
        {
            ConnectionInfo = LoadConnectionInfo(connectionFile);
            _session.Key = ConnectionInfo.key;

            _context = new ZContext();            

            ShellSocket = new ZSocket(_context, ZSocketType.DEALER);
            ShellSocket.IdentityString = _session.Id;
            ShellSocket.Linger = TimeSpan.FromMilliseconds(1000);
            ShellSocket.ReceiveTimeout = TimeSpan.FromSeconds(3);
            ShellSocket.Connect($"tcp://localhost:{ConnectionInfo.shell_port}");

            ControlSocket = new ZSocket(_context, ZSocketType.DEALER);
            ControlSocket.IdentityString = _session.Id;
            ControlSocket.Linger = TimeSpan.FromMilliseconds(1000);
            ControlSocket.ReceiveTimeout = TimeSpan.FromSeconds(3);
            ControlSocket.Connect($"tcp://localhost:{ConnectionInfo.control_port}");

            IoPubSocket = new ZSocket(_context, ZSocketType.SUB);
            IoPubSocket.SetOption(ZSocketOption.SUBSCRIBE, "");
            IoPubSocket.IdentityString = _session.Id;
            IoPubSocket.Linger = TimeSpan.FromMilliseconds(1000);
            IoPubSocket.Connect($"tcp://localhost:{ConnectionInfo.iopub_port}");

            StdInSocket = new ZSocket(_context, ZSocketType.DEALER);
            StdInSocket.IdentityString = _session.Id;
            StdInSocket.Linger = TimeSpan.FromMilliseconds(1000);
            StdInSocket.Connect($"tcp://localhost:{ConnectionInfo.stdin_port}");

            KernelInfo = GetKernelInfo();

            OnConnected?.Invoke(this, new EventArgs());
        }

        public JupyterMessage.ShutdownReplyContent Shutdown(bool restart = false)
        {
            OnShutdown?.Invoke(this, new EventArgs());
            if (ContinueShutdown != null)
            {
                ContinueShutdown.Wait();
            }
            var result = ExecuteControl(JupyterMessage.Header.MsgType.shutdown_request, new JupyterMessage.ShutdownRequestContent
            {
                restart= restart
            });
            var response = result.content as JupyterMessage.ShutdownReplyContent;

            var ownKernel = _kernelManager != null;
            if (ownKernel)
                _kernelManager.StopKernel();

            return response;
        }

        private JupyterMessage.KernelInfoReplyContent GetKernelInfo()
        {
            var result = ExecuteShell(JupyterMessage.Header.MsgType.kernel_info_request, new JupyterMessage.KernelInfoRequestContent());
            return result.content as JupyterMessage.KernelInfoReplyContent;
        }

        //see interrupt_kernel in manager.py
        public void KernelInterrupt()
        {
            if (KernelSpec.spec.interrupt_mode == "interrupt_mode")
            {
                ExecuteControl(JupyterMessage.Header.MsgType.interrupt_request, new JupyterMessage.KernelInterruptRequestContent());
            }
            else
            {
                _kernelManager.SendInterrupt();
            }
        }

        public void SendInputReply(string value)
        {
            var replyContent = new JupyterMessage.InputReplyContent
            {
                value = value
            };
            var toSend = _session.Serilize(_session.BuildMessage(JupyterMessage.Header.MsgType.input_reply, replyContent));
            StdInSocket.Send(toSend);
        }

        protected void SendShell(JupyterMessage.Header.MsgType msgType, JupyterMessage.Content messageContent, CellBase cell)
        {
            var message = _session.BuildMessage(msgType, messageContent);
            if (cell != null)
            {
                cell.SetMsgId(message.header.msg_id);
            }
            ShellSocket.Send(_session.Serilize(message));
        }

        protected JupyterMessage ExecuteShell(
            JupyterMessage.Header.MsgType msgType, 
            JupyterMessage.Content messageContent)
        {
            var message = _session.BuildMessage(msgType, messageContent);
            ShellSocket.Send(_session.Serilize(message));
            var replyMessage = ShellSocket.ReceiveMessage();
            return ParseMessage(replyMessage);
        }

        protected JupyterMessage ExecuteControl(
            JupyterMessage.Header.MsgType msgType,
            JupyterMessage.Content messageContent)
        {
            var message = _session.BuildMessage(msgType, messageContent);
            ControlSocket.Send(_session.Serilize(message));
            var replyMessage = ControlSocket.ReceiveMessage();
            return ParseMessage(replyMessage);
        }

        protected void FlushMessages()
        {
            var poll = ZPollItem.CreateReceiver();
            ZMessage incoming;
            ZError error;
            while (StdInSocket.PollIn(poll, out incoming, out error, TimeSpan.FromMilliseconds(100))) { }
            while (IoPubSocket.PollIn(poll, out incoming, out error, TimeSpan.FromMilliseconds(100))) { }
            while (ShellSocket.PollIn(poll, out incoming, out error, TimeSpan.FromMilliseconds(100))) { }
        }

        protected void ProcessMessage(ZMessage incoming, ZError error)
        {
            if (error != null)
            {
                Debug.WriteLine("ProcessMessage error :-( " + error.ToString());
                return;
            }

            var msg = ParseMessage(incoming);
            if (msg.header == null)
            {
                return;
            }

            switch (msg.header.msg_type)
            {
                case JupyterMessage.Header.MsgType.input_request:
                    var content = (JupyterMessage.InputRequestContent)msg.content;
                    OnInputRequest?.Invoke(this, (content.prompt, content.password));
                    break;

                case JupyterMessage.Header.MsgType.status:
                    CurrentState = ((JupyterMessage.StatusContent)msg.content).execution_state;
                    OnStatus?.Invoke(this, CurrentState);
                    break;

                case JupyterMessage.Header.MsgType.execute_result:
                case JupyterMessage.Header.MsgType.execute_input:
                case JupyterMessage.Header.MsgType.error:
                case JupyterMessage.Header.MsgType.stream:
                case JupyterMessage.Header.MsgType.display_data:
                case JupyterMessage.Header.MsgType.execute_reply:
                case JupyterMessage.Header.MsgType.shutdown_reply:
                case JupyterMessage.Header.MsgType.interrupt_reply:
                case JupyterMessage.Header.MsgType.inspect_reply:
                case JupyterMessage.Header.MsgType.complete_reply:
                case JupyterMessage.Header.MsgType.comm_open:
                case JupyterMessage.Header.MsgType.comm_close:
                case JupyterMessage.Header.MsgType.comm_msg:
                    OnOutputMessage?.Invoke(this, msg);
                    break;

                default:
                    Debug.WriteLine($"Invalid message received {msg.header.msg_type}");
                    break;
            }
        }

        protected JupyterMessage ParseMessage(ZMessage message)
        {
            const string delimiter = "<IDS|MSG>";
            var msgWithoutIdentities = message
                .Select(x => x.ToString())
                .SkipWhile(x => x != delimiter)
                .ToList();

            if (msgWithoutIdentities.Count < 6)
            {
                Debug.WriteLine("Received invalid message, ignoring... " + message);
                return new JupyterMessage();
            }

            var result = new JupyterMessage();
            result.header = JsonConvert.DeserializeObject<JupyterMessage.Header>(msgWithoutIdentities[2]);
            result.parent_header = JsonConvert.DeserializeObject<JupyterMessage.Header>(msgWithoutIdentities[3]);
            switch (result.header.msg_type)
            {

                case JupyterMessage.Header.MsgType.status:
                    result.content = JsonConvert.DeserializeObject<JupyterMessage.StatusContent>(msgWithoutIdentities[5]);
                    break;

                case JupyterMessage.Header.MsgType.execute_reply:
                    result.content = JsonConvert.DeserializeObject<JupyterMessage.ExecuteReplyContent>(msgWithoutIdentities[5]);
                    break;

                case JupyterMessage.Header.MsgType.execute_input:
                    result.content = JsonConvert.DeserializeObject<JupyterMessage.ExecuteInputContent>(msgWithoutIdentities[5]);
                    break;

                case JupyterMessage.Header.MsgType.execute_result:
                    result.content = JsonConvert.DeserializeObject<JupyterMessage.ExecuteResultContent>(msgWithoutIdentities[5]);
                    break;

                case JupyterMessage.Header.MsgType.kernel_info_reply:
                    result.content = JsonConvert.DeserializeObject<JupyterMessage.KernelInfoReplyContent>(msgWithoutIdentities[5]);
                    break;

                case JupyterMessage.Header.MsgType.error:
                    result.content = JsonConvert.DeserializeObject<JupyterMessage.ErrorContent>(msgWithoutIdentities[5]);
                    break;

                case JupyterMessage.Header.MsgType.stream:
                    result.content = JsonConvert.DeserializeObject<JupyterMessage.StreamContent>(msgWithoutIdentities[5]);
                    break;

                case JupyterMessage.Header.MsgType.display_data:
                    result.content = JsonConvert.DeserializeObject<JupyterMessage.DisplayDataContent>(msgWithoutIdentities[5]);
                    break;

                case JupyterMessage.Header.MsgType.input_request:
                    result.content = JsonConvert.DeserializeObject<JupyterMessage.InputRequestContent>(msgWithoutIdentities[5]);
                    break;

                case JupyterMessage.Header.MsgType.shutdown_reply:
                    result.content = JsonConvert.DeserializeObject<JupyterMessage.ShutdownReplyContent>(msgWithoutIdentities[5]);
                    break;

                case JupyterMessage.Header.MsgType.inspect_reply:
                    result.content = JsonConvert.DeserializeObject<JupyterMessage.InspectReplyContent>(msgWithoutIdentities[5]);
                    break;

                case JupyterMessage.Header.MsgType.is_complete_reply:
                    result.content = JsonConvert.DeserializeObject<JupyterMessage.IsCompleteReply>(msgWithoutIdentities[5]);
                    break;

                case JupyterMessage.Header.MsgType.comm_open:
                    result.content = JsonConvert.DeserializeObject<JupyterMessage.CommOpenContent>(msgWithoutIdentities[5]);
                    break;

                case JupyterMessage.Header.MsgType.comm_close:
                    result.content = JsonConvert.DeserializeObject<JupyterMessage.CommCloseContent>(msgWithoutIdentities[5]);
                    break;

                case JupyterMessage.Header.MsgType.comm_msg:
                    result.content = JsonConvert.DeserializeObject<JupyterMessage.CommMsgContent>(msgWithoutIdentities[5]);
                    break;

                case JupyterMessage.Header.MsgType.complete_reply:
                    result.content = JsonConvert.DeserializeObject<JupyterMessage.CompleteReply>(msgWithoutIdentities[5]);
                    break;
            }
            return result;
        }

        private ConnectionInfo LoadConnectionInfo(string connectionFile) =>
            JsonConvert.DeserializeObject<ConnectionInfo>(File.ReadAllText(connectionFile));
    }
}
