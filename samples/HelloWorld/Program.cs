using JupyterNetClient;
using JupyterNetClient.Nbformat;
using System;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        //Initializing the Jupyter client
        //The constructor of JupyterBlockingClient will throw an execption if the jupyter framework is not found
        //It is searched on the folders defined on the PATH system variable
        //You can also pass the folder where python.exe is located as an argument of the constructor
        // (since the jupyter framework is located on the python folder)
        var client = new JupyterBlockingClient();

        //Getting available kernels
        var kernels = client.GetKernels();
        if (kernels.Count == 0)
            throw new Exception("No kernels found");

        //Connecting to the first kernel found
        Console.WriteLine($"Connecting to kernel {kernels.First().Value.spec.display_name}");
        client.StartKernel(kernels.First().Key);
        Console.WriteLine("Connected\n");

        //A callback that is executed when there is any information that needs to be shown to the user
        client.OnOutputMessage += Client_OnOutputMessage;

        //Executing some code
        client.Execute("print(\"Hello from Jupyter\")");

        //Closing the kernel
        client.Shutdown();
           
        Console.WriteLine("Press enter to exit");
        Console.ReadLine();            
    }

    private static void Client_OnOutputMessage(object sender, JupyterMessage message)
    {
        switch (message.content)
        {
            case JupyterMessage.ExecuteResultContent executeResultContent:
                Console.WriteLine($"[{executeResultContent.execution_count}] - {executeResultContent.data[MimeTypes.TextPlain]}");
                break;

            case JupyterMessage.StreamContent streamContent:
                Console.WriteLine(streamContent.text);
                break;

            default:
                break;
        }
    }    
}