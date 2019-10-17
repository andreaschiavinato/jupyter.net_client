using JupiterNetClient;
using JupiterNetClient.Nbformat;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace JupyterNetSampleConsole
{
    public class ConsoleAsync
    {
        public static void Execute()
        {
            var client = new JupyterClient();
            var kernels = client.GetKernels();

            if (kernels.Count == 0)
                throw new Exception("No kernels found");

            Console.WriteLine($"Connecting to kernel {kernels.First().Value.spec.display_name}");
            client.StartKernel(kernels.First().Key);

            DisplayKernelInfo(client.KernelInfo);

            client.OnOutputMessage += Client_OnOutputMessage;
            client.OnStatus += Client_OnStatus;

            Console.WriteLine("");
            Console.WriteLine("");

            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
            var task = new Task(() =>
            {
                while (true)
                {
                    Console.WriteLine("Enter code to execute or EXIT to terminate:");
                    var code = Console.ReadLine();
                    if (code == "EXIT")
                    {
                        tcs.SetResult(string.Empty);
                    }
                    else
                    {
                        client.Execute(code);
                        //client.BeginInspect(code, 0, 1);
                    }
                }
            });

            task.Start();

            var result = tcs.Task.Result;

            Console.WriteLine("SHUTTING DOWN KERNEL");
            client.Shutdown();

            Console.WriteLine("DONE");
        }

        private static void Client_OnStatus(object sender, KernelState e)
        {
            Console.WriteLine($"Status: {e}");
        }

        private static void Client_OnOutputMessage(object sender, JupyterMessage message)
        {
            switch (message.header.msg_type)
            {
                case JupyterMessage.Header.MsgType.execute_input:
                    var executeInputContent = (JupyterMessage.ExecuteInputContent)message.content;
                    Console.WriteLine($"Execute input  [{executeInputContent.execution_count}] - {executeInputContent.code}");
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

                case JupyterMessage.Header.MsgType.inspect_reply:
                    var inspectReplyContent = (JupyterMessage.InspectReplyContent)message.content;
                    Console.WriteLine($"Inspect reply  {inspectReplyContent.status} - {(inspectReplyContent.found ? inspectReplyContent.data[MimeTypes.TextPlain] : string.Empty)}");
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
}
