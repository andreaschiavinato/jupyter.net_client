using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace JupiterNetClient
{
    public class KernelManager
    {
        public KernelSpecList KernelSpecs;

        private Process _kernelProcess;
        private string _pythonFolder;        
        private KernelSpec _kernelSpec;        

        public void Initialize()
        {
            _pythonFolder = FindPythonFolder();
            KernelSpecs = GetKernels(_pythonFolder);
        }

        public string StartKernel(string sessionId, string kernelName)
        {
            _kernelSpec = KernelSpecs.kernelspecs[kernelName];

            var connectionFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "jupyter",
                "runtime",
                sessionId + ".json");
            
            var kernelExe = _kernelSpec.spec.argv[0];
            var kernalArgs = _kernelSpec.spec.argv
                .Skip(1)
                .Aggregate(string.Empty, (a, b) => a + " " + b)
                .Replace("{connection_file}", connectionFile);
            
            _kernelProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = kernelExe,
                    Arguments = kernalArgs,
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    RedirectStandardInput = true,
                    CreateNoWindow = true
                }
            };

            _kernelProcess.Start();
            
            WaitConnectionFileWritten(connectionFile);
            return connectionFile;
        }

        public void SendInterrupt()
        {
            //Send CTRL+C to kernel process
            if (AttachConsole((uint)_kernelProcess.Id))
            {
                SetConsoleCtrlHandler(null, true);
                GenerateConsoleCtrlEvent(CtrlTypes.CTRL_C_EVENT, 0);
                FreeConsole();
            }
        }

        public void StopKernel() => 
            _kernelProcess.Close();

        private static string FindPythonFolder()
        {
            var s = Environment.GetEnvironmentVariable("PATH");
            var filename = "python.exe";
            foreach (var dir in s.Split(';'))
            {
                if (File.Exists(Path.Combine(dir, filename)))
                    return dir;
            }
            return null;
        }

        private static KernelSpecList GetKernels(string pythonFolder)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(pythonFolder, "Scripts", "jupyter-kernelspec.exe"),
                    Arguments = "list --json",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();

            StreamReader reader = process.StandardOutput;
            var kernelSpecJson = reader.ReadToEnd();

            return JsonConvert.DeserializeObject<KernelSpecList>(kernelSpecJson);
        }

        private void WaitConnectionFileWritten(string file)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            var timeout = TimeSpan.FromSeconds(10);
            while (!File.Exists(file) && stopWatch.Elapsed < timeout)
            {
                Thread.Sleep(250);
            }
            Thread.Sleep(500);
            Console.WriteLine($"Connection file ({file}) found: {File.Exists(file)}");
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);

        private delegate bool ConsoleCtrlDelegate(CtrlTypes CtrlType);

        // Enumerated type for the control messages sent to the handler routine
        private enum CtrlTypes : uint
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GenerateConsoleCtrlEvent(CtrlTypes dwCtrlEvent, uint dwProcessGroupId);
    }

}