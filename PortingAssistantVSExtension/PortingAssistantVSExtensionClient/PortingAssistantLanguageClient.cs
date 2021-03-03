using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace PortingAssistantVSExtensionClient
{
    [ContentType("csharp")]
    [Export(typeof(ILanguageClient))]
    class PortingAssistantLanguageClient : ILanguageClient, ILanguageClientCustomMessage
    {
        public string Name => "Porting Assistant Extension";

        public IEnumerable<string> ConfigurationSections => new[] { "csharp" };
        public object InitializationOptions => null;
        public IEnumerable<string> FilesToWatch => null;
        public object MiddleLayer => null;
        public object CustomMessageTarget
        {
            get;
            set;
        }


        public event AsyncEventHandler<EventArgs> StartAsync;
        public event AsyncEventHandler<EventArgs> StopAsync;

        public PortingAssistantLanguageClient()
        {
            Instance = this;
        }

        internal static PortingAssistantLanguageClient Instance
        {
            get;
            set;
        }

        internal JsonRpc PortingAssistantRpc
        {
            get;
            set;
        }


        public async Task<Connection> ActivateAsync(CancellationToken token)
        {
            var stdInPipeName = @"clientreadpipe";
            var stdOutPipeName = @"clientwritepipe";
            var readerPipe = new NamedPipeClientStream(serverName: ".", pipeName: stdInPipeName, direction: PipeDirection.In, options: PipeOptions.Asynchronous);
            var writerPipe = new NamedPipeClientStream(serverName: ".", pipeName: stdOutPipeName, direction: PipeDirection.Out, options: PipeOptions.Asynchronous);
            var serverPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "PortingAssistantLanguageServer", @"PortingAssistantExtensionServer.exe");
            if (File.Exists(serverPath))
            {
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = serverPath;
                info.WorkingDirectory = Path.GetDirectoryName(serverPath);
                Process process = new Process();
                process.StartInfo = info;
                if (process.Start())
                {
                    await readerPipe.ConnectAsync(token);
                    await writerPipe.ConnectAsync(token);
                    return new Connection(readerPipe, writerPipe);
                }
                else
                {
                    return null;
                }
            }
            else
            {
#if DEBUG
                await readerPipe.ConnectAsync(token);
                await writerPipe.ConnectAsync(token);
                return new Connection(readerPipe, writerPipe);
#else
                return null;
#endif
            }

        }

        public async Task OnLoadedAsync()
        {
            await StartAsync?.InvokeAsync(this, EventArgs.Empty);
        }

        public Task OnServerInitializedAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnServerInitializeFailedAsync(Exception e)
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public async Task AttachForCustomMessageAsync(JsonRpc rpc)
        {
            this.PortingAssistantRpc = rpc;
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        }

        //public async Task AttachForCustomMessageAsync(JsonRpc rpc)
        //{
        //    this.Rpc = rpc;

        //    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        //    // Sets the UI context so the custom command will be available.
        //    var monitorSelection = ServiceProvider.GlobalProvider.GetService(typeof(IVsMonitorSelection)) as IVsMonitorSelection;
        //    if (monitorSelection != null)
        //    {
        //        if (monitorSelection.GetCmdUIContextCookie(ref this.uiContextGuid, out uint cookie) == VSConstants.S_OK)
        //        {
        //            monitorSelection.SetCmdUIContext(cookie, 1);
        //        }
        //    }
        //}


    }


}
