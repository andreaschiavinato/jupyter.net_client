using JupyterNetClient;
using JupyterNetClient.Nbformat;
using System;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var client = new JupyterBlockingClient();

        //Getting available kernels
        var kernels = client.GetKernels();
        if (kernels.Count == 0)
            throw new Exception("No kernels found");

        //Connecting to the first kernel found
        Console.WriteLine($"Connecting to kernel {kernels.First().Value.spec.display_name}");
        client.StartKernel(kernels.First().Key);
        Console.WriteLine("Connected");

        //Creating a notebook and adding a code cell
        var nb = new Notebook(client.KernelSpec, client.KernelInfo.language_info);
        var cell = nb.AddCode("print(\"Hello from Jupyter\")");

        //Setting up the callback so that the outputs are written on the notebook
        client.OnOutputMessage += (sender, message) => { if (ShouldWrite(message)) cell.AddOutputFromMessage(message); };
            
        //executing the code
        client.Execute(cell.source);

        //saving the notebook
        nb.Save("test.ipynb");
        Console.WriteLine("File test.ipynb written");

        //Closing the kernel
        client.Shutdown();

        Console.WriteLine("Press enter to exit");
        Console.ReadLine();
    }

    private static bool ShouldWrite(JupyterMessage message) =>
        message.header.msg_type == JupyterMessage.Header.MsgType.execute_result
        || message.header.msg_type == JupyterMessage.Header.MsgType.display_data
        || message.header.msg_type == JupyterMessage.Header.MsgType.stream
        || message.header.msg_type == JupyterMessage.Header.MsgType.error;
}
