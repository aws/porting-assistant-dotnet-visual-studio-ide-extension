using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Newtonsoft.Json.Linq;
using PortingAssistantVSExtensionClient.Common;
using PortingAssistantVSExtensionClient.Options;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
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

        private readonly string AssemblyPath;

        private readonly string ConfigurationFileName;

        public readonly string LanguageServerPath;

        public readonly string ConfigurationPath;

        public const string PackageGuidString = "f41a71b0-3e17-4342-892d-aabc368ee8e8";
        public string Name => Common.Constants.ApplicationName;

        public IEnumerable<string> ConfigurationSections => null;
        public object InitializationOptions => JObject.FromObject(new {
            paSettings = new Models.UpdateSettingsRequest()
            {
                EnabledContinuousAssessment = UserSettings.Instance.EnabledContinuousAssessment,
                EnabledMetrics = UserSettings.Instance.EnabledMetrics,
                AWSProfileName = UserSettings.Instance.AWSProfileName,
            }
        });

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
            this.AssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            this.ConfigurationFileName = Common.Constants.DefaultConfigurationFile;
            this.LanguageServerPath = Path.Combine(
                AssemblyPath, 
                Common.Constants.ApplicationServerLocation,
                Common.Constants.ApplicationServerName);
            this.ConfigurationPath = "\"" + Path.Combine(
                AssemblyPath,
                Common.Constants.ResourceFolder,
                ConfigurationFileName) + "\"";
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

#if Dev17
        bool ILanguageClient.ShowNotificationOnInitializeFailed => true;
#endif
        public static async Task UpdateUserSettingsAsync()
        {
            if (Instance == null || Instance.PortingAssistantRpc == null) return;
            var request = new Models.UpdateSettingsRequest()
            {
                EnabledContinuousAssessment = UserSettings.Instance.EnabledContinuousAssessment,
                EnabledMetrics = UserSettings.Instance.EnabledMetrics,
                AWSProfileName = UserSettings.Instance.AWSProfileName,
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
                var extensionVersion = GetExtensionVersion();
                var (readerPipe, writerPipe) = CreateConnectionPipe(stdInPipeName, stdOutPipeName);
                
                if (File.Exists(LanguageServerPath))
                {
                    ProcessStartInfo info = new ProcessStartInfo()
                    {
                        FileName = LanguageServerPath,
                        WorkingDirectory = Path.GetDirectoryName(LanguageServerPath),
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        Arguments = $"{ConfigurationPath} {stdOutPipeName} {stdInPipeName} {extensionVersion}"
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
                Trace.WriteLine($"Failed to activate server. {e}");
                throw;
            }

        }

        private (NamedPipeServerStream readerPipe, NamedPipeServerStream writerPipe) CreateConnectionPipe(string stdInPipeName, string stdOutPipeName)
        {
            var readerPipe = new NamedPipeServerStream(stdInPipeName, PipeDirection.In, maxNumberOfServerInstances: 5, transmissionMode: PipeTransmissionMode.Byte, options: System.IO.Pipes.PipeOptions.Asynchronous, inBufferSize: 256, outBufferSize: 256);
            var writerPipe = new NamedPipeServerStream(stdOutPipeName, PipeDirection.Out, maxNumberOfServerInstances: 5, transmissionMode: PipeTransmissionMode.Byte, options: System.IO.Pipes.PipeOptions.Asynchronous, inBufferSize:256, outBufferSize:256);
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

        private string GetExtensionVersion()
        {
            try
            {
                var doc = new XmlDocument();
                doc.Load(Path.Combine(AssemblyPath, "extension.vsixmanifest"));
                var metaData = doc.DocumentElement.ChildNodes.Cast<XmlElement>().First(x => x.Name == "Metadata");
                var identity = metaData.ChildNodes.Cast<XmlElement>().First(x => x.Name == "Identity");
                return identity.GetAttribute("Version");
            }catch(Exception e)
            {
                Trace.WriteLine($"Cannot identify extension version. {e}");
                return "0.0.1";
            }
           
        }
#if Dev17
        public Task<InitializationFailureContext> OnServerInitializeFailedAsync(ILanguageClientInitializationInfo initializationState)
        {
            var error = initializationState;
            return (Task<InitializationFailureContext>)Task.CompletedTask;
        }
#endif
    }
}
