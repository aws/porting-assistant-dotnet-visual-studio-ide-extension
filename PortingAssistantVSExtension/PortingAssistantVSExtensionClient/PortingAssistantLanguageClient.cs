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
    [ContentType("CSharpFileType")]
    [Export(typeof(ILanguageClient))]
    class PortingAssistantLanguageClient : ILanguageClient, ILanguageClientCustomMessage
    {
        public string Name => Common.Constants.ApplicationName;

        public IEnumerable<string> ConfigurationSections => new[] { "CSharpFileType", "paconfig" };
        public object InitializationOptions => null;
        public IEnumerable<string> FilesToWatch => null;
        public object MiddleLayer => null;
        public object CustomMessageTarget
        {
            get;
            set;
        }


        [Import]
        internal IContentTypeRegistryService ContentTypeRegistryService { get; set; }


        public event AsyncEventHandler<EventArgs> StartAsync;
        public event AsyncEventHandler<EventArgs> StopAsync;

        public static bool ServerStarted = false;

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
            var stdInPipeName = Common.Constants.DebugInPipeName;
            var stdOutPipeName = Common.Constants.DebugOutPipeName;
            var readerPipe = new NamedPipeServerStream(stdInPipeName, PipeDirection.In, maxNumberOfServerInstances: 1, transmissionMode: PipeTransmissionMode.Byte, options: System.IO.Pipes.PipeOptions.Asynchronous);
            var writerPipe = new NamedPipeServerStream(stdOutPipeName, PipeDirection.Out, maxNumberOfServerInstances: 1, transmissionMode: PipeTransmissionMode.Byte, options: System.IO.Pipes.PipeOptions.Asynchronous);
            var serverPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), Common.Constants.ApplicationServerLocation, @"PortingAssistantExtensionServer.exe");
            if (File.Exists(serverPath))
            {
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = serverPath;
                info.WorkingDirectory = Path.GetDirectoryName(serverPath);
                info.Arguments = stdInPipeName + " " + stdOutPipeName;
                Process process = new Process();
                process.StartInfo = info;
                if (process.Start())
                {
                    await readerPipe.WaitForConnectionAsync(token);
                    await writerPipe.WaitForConnectionAsync(token);
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
                await readerPipe.WaitForConnectionAsync(token);
                await writerPipe.WaitForConnectionAsync(token);
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
            ServerStarted = true;
            return Task.CompletedTask;
        }

        public Task OnServerInitializeFailedAsync(Exception e)
        {
            ServerStarted = false;
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public async Task AttachForCustomMessageAsync(JsonRpc rpc)
        {
            this.PortingAssistantRpc = rpc;
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        }


    }


}
