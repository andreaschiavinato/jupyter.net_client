using JupyterNetClient;
using JupyterNetClient.Nbformat;
using System;
using System.Linq;
using System.Threading.Tasks;

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
        Console.WriteLine("Connected\n");

        //Loading a script containing the definition of the function do_something
        client.Execute("%run script.py");

        //Creating an event handler that stores the result of the computation in a TaskCompletionSource object 
        var promise = new TaskCompletionSource<string>();
        EventHandler<JupyterMessage> hanlder = (sender, message) =>
        {
            if (message.header.msg_type == JupyterMessage.Header.MsgType.execute_result)
            {
                var content = (JupyterMessage.ExecuteResultContent)message.content;
                promise.SetResult(content.data[MimeTypes.TextPlain]);
            }
            else if (message.header.msg_type == JupyterMessage.Header.MsgType.error)
            {
                var content = (JupyterMessage.ErrorContent)message.content;
                promise.SetException(new Exception($"Jupyter kenel error: {content.ename} {content.evalue}"));
            }
        };
        client.OnOutputMessage += hanlder;
        //calling the function do_something
        client.Execute("do_something(2)");
        //removing event handler, since the TaskCompletionSource cannot be reused
        client.OnOutputMessage -= hanlder;

        //getting the result
        try
        {
            Console.WriteLine("Result:");
            if (promise.Task.IsCompleted)
                Console.WriteLine(promise.Task.Result);
            else
                Console.WriteLine("No result received");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        finally
        {
            //Closing the kernel
            client.Shutdown();
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }
    }
}