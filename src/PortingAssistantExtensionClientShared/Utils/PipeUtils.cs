using Microsoft.VisualStudio.Shell;
using PortingAssistantVSExtensionClient.Commands;
using PortingAssistantVSExtensionClient.Options;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace PortingAssistantVSExtensionClient.Utils
{
    class PipeUtils
    {
        public static void StartListenerConnection(string pipeName, Func<Task> taskToRun)
        {
            System.Threading.Tasks.Task.Factory.StartNew(async () =>
            {
                NamedPipeServerStream server = null;
                try
                {
                    server = new NamedPipeServerStream(pipeName);

                    await server.WaitForConnectionAsync();
                    await ThreadHelper.JoinableTaskFactory.RunAsync(taskToRun);
                }
                catch (Exception)
                {

                }
                finally
                {
                    if (server != null)
                    {
                        if (server.IsConnected)
                        {
                            server.Disconnect();
                            server.Close();
                        }
                        server.Dispose();
                    }
                }
            });
        }
    }
}
