using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PortingAssistantExtensionIntegTests
{
    class ProcessHelper
    {
        static ProcessHelper _instance = new ProcessHelper();
        private const string SERVER_NAME = "PortingAssistantExtensionServer.exe";
        private Process serverProcess = null;
        private TaskCompletionSource<bool> eventHandled;

        public static ProcessHelper getInstance()
        {
            return _instance;
        }

        private ProcessHelper()
        {

        }

        public void StopServer()
        {
            if (serverProcess != null && !serverProcess.HasExited)
            {
               serverProcess.Kill();
            }
        }

        public async Task StartServer(string wdir, string arg)
        {
            if (serverProcess != null && !serverProcess.HasExited) return;

            eventHandled = new TaskCompletionSource<bool>();
            serverProcess = new Process();
            try
            {
                serverProcess.StartInfo.FileName = SERVER_NAME;
                serverProcess.StartInfo.WorkingDirectory = wdir;
                serverProcess.StartInfo.Verb = "Porting Assistant Language Server";
                serverProcess.StartInfo.Arguments = arg;
                serverProcess.StartInfo.CreateNoWindow = true;
                serverProcess.EnableRaisingEvents = true;
                serverProcess.Exited += new EventHandler(myProcess_Exited);
                serverProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

                ThreadStart ths = new ThreadStart(delegate () {
                    serverProcess.Start();
                    serverProcess.WaitForExit();
                });

                Thread th = new Thread(ths);
                th.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred trying to print \"{SERVER_NAME}\":\n{ex.Message}");
                throw ex;
            }
        }

        // Handle Exited event and display process information.
        private void myProcess_Exited(object sender, System.EventArgs e)
        {
            Console.WriteLine(
                $"Exit time    : {serverProcess.ExitTime}\n" +
                $"Exit code    : {serverProcess.ExitCode}\n" +
                $"Elapsed time : {Math.Round((serverProcess.ExitTime - serverProcess.StartTime).TotalMilliseconds)}");
            eventHandled.TrySetResult(true);
        }
    }
}
