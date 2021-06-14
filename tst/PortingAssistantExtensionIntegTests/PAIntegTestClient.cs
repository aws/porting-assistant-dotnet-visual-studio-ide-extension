using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Client;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using PortingAssistant.Client.Model;
using PortingAssistantExtensionServer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace PortingAssistantExtensionIntegTests
{
    class PAIntegTestClient
    {
        ILanguageClient Client;
        public List<Diagnostic> Diagnostics;
        public string SolutionRootPathUri { get; set; }
        public string SolutionRootPath { get; set;  }
        public string SolutionPath { get; set;  }
        
        public PAIntegTestClient(string solutionPath, string solutionName)
        {
            Diagnostics = new List<Diagnostic>();
            SolutionPath = Path.Combine(solutionPath, solutionName);
            SolutionRootPathUri = "file:\\" + solutionPath;
            SolutionRootPath = solutionPath;
        }

        public async Task InitClientAsync() { 
            var stdInPipeName = "extensionclientreadpipe";
            var stdOutPipeName = "extensionclientwritepipe";

            var clientOptionsAction = new Action<LanguageClientOptions>(option => { });
            var (readerPipe, writerPipe) = await CreateConnectionPipeAsync(stdInPipeName, stdOutPipeName);

            Client = LanguageClient.PreInit(options =>
            {
                options
                .WithRequestProcessIdentifier(new ParallelRequestProcessIdentifier())
                .WithWorkspaceFolder(SolutionRootPathUri, "testFolder")
                .WithInput(readerPipe)
                .WithOutput(writerPipe)
                .WithTrace(InitializeTrace.Verbose)
                .WithRootPath(SolutionRootPath)
                .WithRootUri(SolutionRootPathUri)
                .WithInitializationOptions(new
                {
                    paSettings = new
                    {
                        EnabledContinuousAssessment = true,
                        EnabledMetrics = true,
                        AWSProfileName = "",
                    }
                });

                options.OnInitialize((client, request, ct) =>
                {
                    Console.WriteLine("send inital request");
                    return Task.CompletedTask;
                })

                .OnInitialized((instance, client, server, ct) =>
                {
                    Console.WriteLine("Client Initialized !");
                    return Task.CompletedTask;
                })

                .OnPublishDiagnostics(diagnosticParams =>
                {
                    Diagnostics.AddRange(diagnosticParams.Diagnostics.Where(d => d != null));
                });
               
                var capabilityTypes = typeof(ICapability).Assembly.GetExportedTypes()
                    .Where(z => typeof(ICapability).IsAssignableFrom(z) && z.IsClass && !z.IsAbstract);
                
                foreach (Type capabilityType in capabilityTypes)
                {
                    options.WithCapability(Activator.CreateInstance(capabilityType, Array.Empty<object>()) as ICapability);
                }
            });

            await Client.Initialize(CancellationToken.None).ConfigureAwait(false);
        }


        async private Task<(NamedPipeServerStream readerPipe, NamedPipeServerStream writerPipe)> 
            CreateConnectionPipeAsync(string stdInPipeName, string stdOutPipeName)
        {
            var readerPipe = new NamedPipeServerStream(stdInPipeName, 
                PipeDirection.In, 
                maxNumberOfServerInstances: 5, 
                transmissionMode: PipeTransmissionMode.Byte, 
                options: System.IO.Pipes.PipeOptions.Asynchronous, 
                inBufferSize: 256, 
                outBufferSize: 256);

            var writerPipe = new NamedPipeServerStream(stdOutPipeName, 
                PipeDirection.Out, 
                maxNumberOfServerInstances: 5, 
                transmissionMode: PipeTransmissionMode.Byte, 
                options: System.IO.Pipes.PipeOptions.Asynchronous, 
                inBufferSize: 256, 
                outBufferSize: 256);
            await readerPipe.WaitForConnectionAsync().ConfigureAwait(true);
            await writerPipe.WaitForConnectionAsync().ConfigureAwait(true);

            return (readerPipe, writerPipe);
        }

        public async Task<AnalysisTestResult> AssessSolutionAsync()
        {
            Diagnostics.Clear();

            string pipeName = Guid.NewGuid().ToString();
            
            var analyzeSolutionRequest = new AnalyzeSolutionRequest()
            {
                solutionFilePath = SolutionPath,
                metaReferences = null,
                PipeName = pipeName,
                settings = new AnalyzerSettings()
                {
                    TargetFramework = "netcoreapp3.1",
                    IgnoreProjects = new List<string>(),
                },
            };
            
            var res = Client.SendRequest<AnalyzeSolutionRequest>("analyzeSolution", analyzeSolutionRequest);
            await res.Returning<AnalyzeSolutionResponse>(CancellationToken.None).ConfigureAwait(true);
            AnalysisTestResult analysisResults = new AnalysisTestResult();

            if (Diagnostics.Count > 0)
            {
                Diagnostics.ForEach(diag =>
                {
                    string path = diag.RelatedInformation.ElementAt(0).Location.Uri.Path;
                    CompatEntry entry = new CompatEntry(Path.GetFileName(path), diag.Code, diag.Message, diag.Range);
                    analysisResults.AddEntry(entry);
                });
            }

            return analysisResults;
        }

        public async Task CleanupAsync()
        {
            await Client.Shutdown();
        }
    }
}
