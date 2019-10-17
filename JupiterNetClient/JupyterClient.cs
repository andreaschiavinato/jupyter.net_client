using JupiterNetClient.Nbformat;
using System;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQ;

namespace JupiterNetClient
{
    public class JupyterClient : JupyterClientBase
    {
        private CancellationTokenSource _monitorChannnelsCts;
        

        public JupyterClient() : base()
        {
            OnConnected += (sender, args) => {
                _monitorChannnelsCts = new CancellationTokenSource();
                var monitorChannnelsTask = new Task(MonitorChannels, TaskCreationOptions.LongRunning);
                monitorChannnelsTask.Start();
            };

            ContinueShutdown = new ManualResetEventSlim(false);
            OnShutdown += (sender, args) => _monitorChannnelsCts.Cancel();
        }

        public void Execute(CodeCell cell) =>
            SendShell(
                JupyterMessage.Header.MsgType.execute_request,
                new JupyterMessage.ExecuteRequestContent
                {
                    code = cell.source,
                    silent = false,
                    store_history = true,
                    user_expressions = null,
                    allow_stdin = true,
                    stop_on_error = false
                },
                cell);

        public void Execute(string code) =>
            SendShell(
                JupyterMessage.Header.MsgType.execute_request,
                new JupyterMessage.ExecuteRequestContent
                {
                    code = code,
                    silent = false,
                    store_history = true,
                    user_expressions = null,
                    allow_stdin = true,
                    stop_on_error = false
                },
                null);

        public void Complete(string code, int cursorPos) => 
            SendShell(
                JupyterMessage.Header.MsgType.complete_request, 
                new JupyterMessage.CompleteRequest { code = code, cursor_pos = cursorPos },
                null);

        public void Inspect(string code, int cursorPos, int detailLevel) =>
            SendShell(
                JupyterMessage.Header.MsgType.inspect_request,
                new JupyterMessage.InspectRequestContent
                {
                    code = code,
                    cursor_pos = cursorPos,
                    detail_level = detailLevel
                },
                null);

        private void MonitorChannels()
        {
            var poll = ZPollItem.CreateReceiver();
            ZMessage incoming;
            ZError error;
            while (!_monitorChannnelsCts.IsCancellationRequested)
            {
                if (_monitorChannnelsCts.IsCancellationRequested)
                    break;

                if (StdInSocket.PollIn(poll, out incoming, out error, TimeSpan.FromMilliseconds(100)))
                {
                    ProcessMessage(incoming, error);
                }

                if (_monitorChannnelsCts.IsCancellationRequested)
                    break;

                if (IoPubSocket.PollIn(poll, out incoming, out error, TimeSpan.FromMilliseconds(100)))
                {
                    ProcessMessage(incoming, error);
                }

                if (_monitorChannnelsCts.IsCancellationRequested)
                    break;

                if (ShellSocket.PollIn(poll, out incoming, out error, TimeSpan.FromMilliseconds(100)))
                {
                    ProcessMessage(incoming, error);
                }

                if (_monitorChannnelsCts.IsCancellationRequested)
                    break;

                if (ControlSocket.PollIn(poll, out incoming, out error, TimeSpan.FromMilliseconds(100)))
                {
                    ProcessMessage(incoming, error);
                }
            }
            ContinueShutdown.Set();
        }
    }
}
