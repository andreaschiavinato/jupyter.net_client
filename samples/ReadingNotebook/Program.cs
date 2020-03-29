using JupyterNetClient.Nbformat;
using System;

class Program
{
    static void Main(string[] args)
    {
        var nb = Notebook.ReadFromFile("test.ipynb");
        Console.WriteLine($"Langauge: {nb.metadata.language_info.name} {nb.metadata.language_info.version}");
        Console.WriteLine($"Kernel: {nb.metadata.kernel_info.name}");
        Console.WriteLine($"Notebook format: {nb.nbformat}.{nb.nbformat_minor}");
        Console.WriteLine("\nContent:\n");
        foreach (var cell in nb.cells)
        {
            switch (cell)
            {
                case MarkdownCell markdownCell:                        
                    Console.WriteLine(markdownCell.source);
                    break;

                case CodeCell codeCell:
                    Console.WriteLine(codeCell.source);
                    foreach (var output in codeCell.outputs)
                    {
                        Console.Write("  " + output.output_type + ": ");
                        switch (output)
                        {
                            case StreamOutputCellOutput streamOutputCellOutput:
                                Console.WriteLine($"{streamOutputCellOutput.name} {streamOutputCellOutput.text}");
                                break;
                            case DisplayDataCellOutput displayDataCellOutput:
                                Console.WriteLine(displayDataCellOutput.data[MimeTypes.TextPlain]);
                                break;
                            case ExecuteResultCellOutput executeResultCellOutput:
                                Console.WriteLine(executeResultCellOutput.data[MimeTypes.TextPlain]);
                                break;
                            case ErrorCellOutput errorCellOutput:
                                Console.WriteLine($"{errorCellOutput.ename} {errorCellOutput.evalue}");
                                break;
                        }                            
                    }
                    break;

                case RawCell _:
                    Console.WriteLine($"(raw cell)");
                    break;
            }
        }

        Console.ReadLine();
    }
}
