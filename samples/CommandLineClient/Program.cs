using JupyterNetClient;
using JupyterNetClient.Nbformat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class Program
{
    private const string Prompt = ">>> ";
    private const string PromptWhite = "... ";
    private static JupyterBlockingClient client;
    private static Dictionary<string, KernelSpec> kernels;

    static void Main(string[] args)
    {
        client = new JupyterBlockingClient();

        kernels = client.GetKernels();
        if (kernels.Count == 0)
            throw new Exception("No kernels found");

        //Connecting to the first kernel found, kernels will contains all the available kerenels
        Console.WriteLine($"Connecting to kernel {kernels.First().Value.spec.display_name}");
        client.StartKernel(kernels.First().Key);

        DisplayKernelInfo(client.KernelInfo);

        client.OnOutputMessage += Client_OnOutputMessage;
        client.OnInputRequest += Client_OnInputRequest;

        //Mainlook asks code to execute and executes it.
        Console.WriteLine("\n\nEnter code to execute or Q <enter> to terminate:");        
        MainLoop(client);

        //terminating the kernel process
        Console.WriteLine("SHUTTING DOWN KERNEL");
        client.Shutdown();
    }

    private static void MainLoop(JupyterBlockingClient client)
    {
        //Using the component ReadLine, which has some nice features like code completion and history support
        ReadLine.HistoryEnabled = true;
        ReadLine.AutoCompletionHandler = new AutoCompletionHandler(client);
        var enteredCode = new StringBuilder();
        var startNewCode = true;
        var lineIdent = string.Empty;
        while (true)
        {

            enteredCode.Append(ReadLine.Read(startNewCode ? Prompt : PromptWhite + lineIdent));
            var code = enteredCode.ToString();
            if (code == "Q")
            {
                //When the user types Q we terminates the application
                return;
            }
            else if (string.IsNullOrWhiteSpace(code))
            {
                //No code entered, do nothing
            }
            else
            {
                //Asking the kernel if the code entered by the user so far is a complete statement.
                // If not, for example because it is the first line of a function definition,
                // we aske the user to enter one more lone
                var isComplete = client.IsComplete(code);
                switch (isComplete.status)
                {
                    case JupyterMessage.IsCompleteStatusEnum.complete:
                        //the code is complete, execute it
                        //the results are given on the OnOutputMessage callback
                        client.Execute(code);
                        startNewCode = true;
                        break;

                    case JupyterMessage.IsCompleteStatusEnum.incomplete:
                        lineIdent = isComplete.indent;
                        enteredCode.Append("\n" + lineIdent);
                        startNewCode = false;
                        break;

                    case JupyterMessage.IsCompleteStatusEnum.invalid:
                    case JupyterMessage.IsCompleteStatusEnum.unknown:
                        Console.WriteLine("Invalid code: " + code);
                        startNewCode = true;
                        break;
                }
            }

            if (startNewCode)
            {
                enteredCode.Clear();
            }
        }
    }

    private static void Client_OnInputRequest(object sender, (string prompt, bool password) e)
    {
        var input = e.password
            ? ReadLine.ReadPassword(e.prompt)
            : ReadLine.Read(e.prompt);
        client.SendInputReply(input);
    }

    private static void Client_OnOutputMessage(object sender, JupyterMessage message)
    {
        switch (message.content)
        {
            case JupyterMessage.ExecuteInputContent executeInputContent:
                Console.WriteLine($"Executing  [{executeInputContent.execution_count}] - {executeInputContent.code}");
                break;

            case JupyterMessage.ExecuteResultContent executeResultContent:
                Console.WriteLine($"Result  [{executeResultContent.execution_count}] - {executeResultContent.data[MimeTypes.TextPlain]}");
                break;

            case JupyterMessage.DisplayDataContent displayDataContent:
                Console.WriteLine($"Data  {displayDataContent.data}");
                break;

            case JupyterMessage.StreamContent streamContent:               
                Console.WriteLine($"Stream  {streamContent.name} {streamContent.text}");
                break;

            case JupyterMessage.ErrorContent errorContent:
                Console.WriteLine($"Error  {errorContent.ename} {errorContent.evalue}");
                Console.WriteLine(errorContent.traceback);
                break;

            case JupyterMessage.ExecuteReplyContent executeReplyContent:
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

    private class AutoCompletionHandler : IAutoCompleteHandler
    {
        private readonly JupyterBlockingClient _client;

        public AutoCompletionHandler(JupyterBlockingClient client)
        {
            _client = client;
        }

        public char[] Separators { get; set; } = new char[] { };

        public string[] GetSuggestions(string text, int index)
        {
            //asking the kernel to provide a list of strins to complete the current line
            var result = _client.Complete(text, text.Length);
            return result.matches
                .Select(s => text.Substring(0, result.cursor_start) + s)
                .ToArray();
        }
    }
}
