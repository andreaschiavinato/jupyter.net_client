using JupiterNetClient;
using JupiterNetClient.Nbformat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JupyterNetSampleConsole
{
    public class ConsoleBlocking
    {
        private const string Prompt = ">>> ";
        private const string PromptWhite = "... ";
        private static JupyterBlockingClient client;
        private static Dictionary<string, KernelSpec> kernels;

        public static void Execute()
        {
            client = new JupyterBlockingClient();
            kernels = client.GetKernels();

            if (kernels.Count == 0)
                throw new Exception("No kernels found");

            Console.WriteLine($"Connecting to kernel {kernels.First().Value.spec.display_name}");
            client.StartKernel(kernels.First().Key);

            DisplayKernelInfo(client.KernelInfo);                      

            client.OnOutputMessage += Client_OnOutputMessage;
            client.OnInputRequest += Client_OnInputRequest;

            Console.WriteLine("\n\nEnter code to execute or Q <enter> to terminate:");
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
            Task.Run(() => MainLoop(client, tcs));
            var unused = tcs.Task.Result;

            Console.WriteLine("SHUTTING DOWN KERNEL");
            client.Shutdown();

            Console.WriteLine("DONE");
        }

        private static void Client_OnInputRequest(object sender, (string prompt, bool password) e)
        {
            var input = e.password
                ? ReadLine.ReadPassword(e.prompt)
                : ReadLine.Read(e.prompt);
            client.SendInputReply(input);
        }

        private static void MainLoop(JupyterBlockingClient client, TaskCompletionSource<string> tcs)
        {
            ReadLine.HistoryEnabled = true;
            ReadLine.AutoCompletionHandler = new AutoCompletionHandler(client);
            var newCode = true;
            var sb = new StringBuilder();
            var lineIdent = string.Empty;
            while (true)
            {
                if (newCode)
                {
                    sb.Clear();
                }
                sb.Append(ReadLine.Read(newCode ? Prompt : PromptWhite + lineIdent));
                var code = sb.ToString();
                if (code == "Q")
                {
                    tcs.SetResult(string.Empty);
                    break;
                }
                else if (string.IsNullOrWhiteSpace(code))
                {
                    //do nothing
                }
                else
                {
                    var isComplete = client.IsComplete(code);
                    switch (isComplete.status)
                    {
                        case JupyterMessage.IsCompleteStatusEnum.complete:
                            client.Execute(code);
                            newCode = true;
                            break;

                        case JupyterMessage.IsCompleteStatusEnum.incomplete:
                            lineIdent = isComplete.indent;
                            sb.Append("\n" + lineIdent);
                            newCode = false;
                            break;

                        case JupyterMessage.IsCompleteStatusEnum.invalid:
                        case JupyterMessage.IsCompleteStatusEnum.unknown:
                            Console.WriteLine("Invalid code: " + code);
                            newCode = true;
                            break;
                    }
                }
            }
        }

        private static void Client_OnOutputMessage(object sender, JupyterMessage message)
        {
            switch (message.header.msg_type)
            {
                case JupyterMessage.Header.MsgType.execute_input:
                    var executeInputContent = (JupyterMessage.ExecuteInputContent)message.content;
                    Console.WriteLine($"Executing  [{executeInputContent.execution_count}] - {executeInputContent.code}");
                    break;

                case JupyterMessage.Header.MsgType.execute_result:
                    var executeResultContent = (JupyterMessage.ExecuteResultContent)message.content;
                    Console.WriteLine($"Result  [{executeResultContent.execution_count}] - {executeResultContent.data[MimeTypes.TextPlain]}");
                    break;

                case JupyterMessage.Header.MsgType.display_data:
                    var displayDataContent = (JupyterMessage.DisplayDataContent)message.content;
                    Console.WriteLine($"Data  {displayDataContent.data}");
                    break;

                case JupyterMessage.Header.MsgType.stream:
                    var streamContent = (JupyterMessage.StreamContent)message.content;
                    Console.WriteLine($"Stream  {streamContent.name} {streamContent.text}");
                    break;

                case JupyterMessage.Header.MsgType.error:
                    var errorContent = (JupyterMessage.ErrorContent)message.content;
                    Console.WriteLine($"Error  {errorContent.ename} {errorContent.evalue}");
                    Console.WriteLine(errorContent.traceback);
                    break;

                case JupyterMessage.Header.MsgType.execute_reply:
                    var executeReplyContent = (JupyterMessage.ExecuteReplyContent)message.content;
                    Console.WriteLine($"Executed  [{executeReplyContent.execution_count}] - {executeReplyContent.status}");
                    break;

                default:
                    break;
            }
        }

        private static void DisplayKernelInfo(JupyterMessage.KernelInfoReplyContent kernelInfo)
        {
            Console.WriteLine("");
            Console.WriteLine(" KERNEL INFO");
            Console.WriteLine("============");
            Console.WriteLine($"Banner: {kernelInfo.banner}");
            Console.WriteLine($"Status: {kernelInfo.status}");
            Console.WriteLine($"Protocol version: {kernelInfo.protocol_version}");
            Console.WriteLine($"Implementation: {kernelInfo.implementation}");
            Console.WriteLine($"Implementation version: {kernelInfo.implementation_version}");
            Console.WriteLine($"Language name: {kernelInfo.language_info.name}");
            Console.WriteLine($"Language version: {kernelInfo.language_info.version}");
            Console.WriteLine($"Language mimetype: {kernelInfo.language_info.mimetype}");
            Console.WriteLine($"Language file_extension: {kernelInfo.language_info.file_extension}");
            Console.WriteLine($"Language pygments_lexer: {kernelInfo.language_info.pygments_lexer}");
            Console.WriteLine($"Language nbconvert_exporter: {kernelInfo.language_info.nbconvert_exporter}");
        }
    }

    internal class AutoCompletionHandler : IAutoCompleteHandler
    {
        private readonly JupyterBlockingClient _client;

        public AutoCompletionHandler(JupyterBlockingClient client)
        {
            _client = client;
        }
        
        public char[] Separators { get; set; } = new char[] { };

        public string[] GetSuggestions(string text, int index)
        {
            var result = _client.Complete(text, text.Length);
            return result.matches
                .Select(s => text.Substring(0, result.cursor_start) + s)
                .ToArray();
        }
    }
}
