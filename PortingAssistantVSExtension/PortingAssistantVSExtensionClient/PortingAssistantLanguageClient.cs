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

    [ContentType("CSharpFileType")]
    [Export(typeof(ILanguageClient))]
    [Guid(PortingAssistantLanguageClient.PackageGuidString)]
    class PortingAssistantLanguageClient : AsyncPackage, ILanguageClient, ILanguageClientCustomMessage2
    {
        private readonly long LaunchTime;

        public readonly string LanguageServerPath;

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
            this.LaunchTime = DateTime.Now.Ticks;
            this.LanguageServerPath = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), 
                Common.Constants.ApplicationServerLocation,
                Common.Constants.ApplicationServerName);
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

        public static async Task UpdateUserSettingsAsync()
        {
            if (Instance == null || Instance.PortingAssistantRpc == null) return;
            var request = new Models.UpdateSettingsRequest()
            {
                EnabledContinuousAssessment = UserSettings.Instance.EnabledContinuousAssessment,
                EnabledMetrics = UserSettings.Instance.EnabledMetrics,
                CustomerEmail = UserSettings.Instance.CustomerEmail,
                RootCacheFolder = UserSettings.Instance.RootCacheFolder
            };
            await Instance.PortingAssistantRpc.InvokeWithParameterObjectAsync<bool>("updateSettings", request);
        }


        public async Task<Connection> ActivateAsync(CancellationToken token)
        {
            await Task.Yield(); 
            try
            {
                UserSettings.Instance.SetLanguageServerStatus(LanguageServerStatus.STARTING);

                var stdInPipeName = Common.Constants.DebugInPipeName;
                var stdOutPipeName = Common.Constants.DebugOutPipeName;
#if DEBUG
                var (debugreaderPipe, debugwriterPipe) = CreateConnectionPipe(stdInPipeName, stdOutPipeName);
                await debugreaderPipe.WaitForConnectionAsync(token).ConfigureAwait(true);
                await debugwriterPipe.WaitForConnectionAsync(token).ConfigureAwait(true);
                return new Connection(debugreaderPipe, debugwriterPipe);
#endif
                stdInPipeName = $"{Common.Constants.InPipeName}{LaunchTime}";
                stdOutPipeName = $"{Common.Constants.OutPipeName}{LaunchTime}";
                var (readerPipe, writerPipe) = CreateConnectionPipe(stdInPipeName, stdOutPipeName);
                
                if (File.Exists(LanguageServerPath))
                {
                    ProcessStartInfo info = new ProcessStartInfo()
                    {
                        FileName = LanguageServerPath,
                        WorkingDirectory = Path.GetDirectoryName(LanguageServerPath),
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        Arguments = $"{stdOutPipeName} {stdInPipeName}"
                    };
                    Process process = new Process { StartInfo = info };
                    if (process.Start())
                    {
                        await readerPipe.WaitForConnectionAsync(token).ConfigureAwait(true);
                        await writerPipe.WaitForConnectionAsync(token).ConfigureAwait(true);
                        return new Connection(readerPipe, writerPipe);
                    }
                }
                return null;
            }
            catch (Exception e)
            {
                throw;
            }

        }

        private (NamedPipeServerStream readerPipe, NamedPipeServerStream writerPipe) CreateConnectionPipe(string stdInPipeName, string stdOutPipeName)
        {
            var readerPipe = new NamedPipeServerStream(stdInPipeName, PipeDirection.In, maxNumberOfServerInstances: 1, transmissionMode: PipeTransmissionMode.Byte, options: System.IO.Pipes.PipeOptions.Asynchronous);
            var writerPipe = new NamedPipeServerStream(stdOutPipeName, PipeDirection.Out, maxNumberOfServerInstances: 1, transmissionMode: PipeTransmissionMode.Byte, options: System.IO.Pipes.PipeOptions.Asynchronous);
            return (readerPipe, writerPipe);
        }

        public async Task OnLoadedAsync()
        {
            await(StartAsync?.InvokeAsync(this, EventArgs.Empty)).ConfigureAwait(false);
        }

        public async Task OnServerInitializedAsync()
        {
            await Task.Yield();
            UserSettings.Instance.SetLanguageServerStatus(LanguageServerStatus.INITIALIZED);
            await UpdateUserSettingsAsync();
        }

        public Task OnServerInitializeFailedAsync(Exception e)
        {
            throw new Exception("On Server Initizile Failed: " + e.Message, e);
        }

        public async Task AttachForCustomMessageAsync(JsonRpc rpc)
        {
            this.PortingAssistantRpc = rpc;
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        }
    }
}
