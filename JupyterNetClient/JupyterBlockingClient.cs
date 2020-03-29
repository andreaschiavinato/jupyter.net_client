using System;
using ZeroMQ;

namespace JupyterNetClient
{
    public class JupyterBlockingClient : JupyterClientBase
    {
        public JupyterBlockingClient(string pythonFolder = "") : base(pythonFolder) { }

        public JupyterMessage Execute(string code)
        {
            FlushMessages();
            CurrentState = KernelState.busy;
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

            var poll = ZPollItem.CreateReceiver();
            ZMessage incoming;
            ZError error;
            while (CurrentState != KernelState.idle)
            {
                if (StdInSocket.PollIn(poll, out incoming, out error, TimeSpan.FromMilliseconds(100)))
                {
                    ProcessMessage(incoming, error);
                }

                if (IoPubSocket.PollIn(poll, out incoming, out error, TimeSpan.FromMilliseconds(100)))
                {
                    ProcessMessage(incoming, error);
                }
            }

            var replyMessage = ShellSocket.ReceiveMessage();
            return ParseMessage(replyMessage);
        }

        public JupyterMessage.IsCompleteReply IsComplete(string code)
        {
            var result = ExecuteShell(JupyterMessage.Header.MsgType.is_complete_request, new JupyterMessage.IsCompleteRequest { code = code });
            return result.content as JupyterMessage.IsCompleteReply;
        }

        public JupyterMessage.CompleteReply Complete(string code, int cursorPos)
        {
            var result = ExecuteShell(JupyterMessage.Header.MsgType.complete_request, new JupyterMessage.CompleteRequest { code = code, cursor_pos = cursorPos });
            return result.content as JupyterMessage.CompleteReply;
        }
    }
}
