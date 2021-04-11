using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using PortingAssistantVSExtensionClient.Common;
using PortingAssistantVSExtensionClient.Options;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace PortingAssistantVSExtensionClient
{
    class CsharpContentDefinition
    {
        [Export]
        [Name("CSharpFileType")]
        [BaseDefinition("CSharp")]
        internal static ContentTypeDefinition CsContentTypeDefinition;

        [Export]
        [FileExtension(".cs")]
        [ContentType("CSharpFileType")]
        internal static FileExtensionToContentTypeDefinition CsFileExtensionDefinition;
    }

    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ContentType("CSharpFileType")]
    [Export(typeof(ILanguageClient))]
    [Guid(PortingAssistantLanguageClient.PackageGuidString)]
    class PortingAssistantLanguageClient : AsyncPackage, ILanguageClient, ILanguageClientCustomMessage2
    {
        public const string PackageGuidString = "f41a71b0-3e17-4342-892d-aabc368ee8e8";
        public string Name => Common.Constants.ApplicationName;

        public IEnumerable<string> ConfigurationSections
        {
            get
            {
                yield return "CSharpFileType";
            }
        }

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
            PAGlobalService.Instance.SetLanguageServerStatus(LanguageServerStatus.STARTING);
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
            PAGlobalService.Instance.SetLanguageServerStatus(LanguageServerStatus.LOADED);
            await StartAsync?.InvokeAsync(this, EventArgs.Empty);
        }

        public async Task OnServerInitializedAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (UserSettings.Instance.EnabledContinuousAssessment) {
                Console.WriteLine("1123");
            }
            PAGlobalService.Instance.SetLanguageServerStatus(LanguageServerStatus.INITIALIZED);
            await Task.CompletedTask;
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
    }
}
