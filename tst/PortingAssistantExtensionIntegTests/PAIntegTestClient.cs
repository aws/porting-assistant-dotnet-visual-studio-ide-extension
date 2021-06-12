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
        string stdInPipeName;
        string stdOutPipeName;

        public PAIntegTestClient(string solutionPath, string solutionName, string inPipe, string outPipe)
        {
            Diagnostics = new List<Diagnostic>();
            SolutionPath = Path.Combine(solutionPath, solutionName);
            SolutionRootPathUri = "file:\\" + solutionPath;
            SolutionRootPath = solutionPath;
            stdInPipeName = inPipe;
            stdOutPipeName = outPipe;
            stdInPipeName = "extensionclientreadpipe";
            stdOutPipeName = "extensionclientwritepipe";
            bool exists = File.Exists(SolutionPath);
            exists = Directory.Exists(solutionPath);
            exists = Directory.Exists(SolutionRootPathUri);

        }

        public async Task InitClient() { 
            //var stdInPipeName = "extensionclientreadpipe";
            //var stdOutPipeName = "extensionclientwritepipe";

            var clientOptionsAction = new Action<LanguageClientOptions>(option => { });
            var (readerPipe, writerPipe) = await CreateConnectionPipe(stdInPipeName, stdOutPipeName);

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
            CreateConnectionPipe(string stdInPipeName, string stdOutPipeName)
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

        public async Task<AnalysisTestResult> AssessSolution()
        {
            //Before
            Diagnostics.Clear();
            //Test
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
            
            //PipeUtils.StartListenerConnection(pipeName, GetAssessmentCompletionTasks( SolutionPath));

            var res = Client.SendRequest<AnalyzeSolutionRequest>("analyzeSolution", analyzeSolutionRequest);
            await res.Returning<AnalyzeSolutionResponse>(CancellationToken.None).ConfigureAwait(true);
            AnalysisTestResult analysisResults = new AnalysisTestResult();

            if (Diagnostics.Count > 0)
            {
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(SolutionRootPath, "File.txt")))
                {
                    Diagnostics.ForEach(diag =>
                    {
                        
                        string path = diag.RelatedInformation.ElementAt(0).Location.Uri.Path;

                        outputFile.WriteLine(Path.GetFileName(path));

                        CompatEntry entry = new CompatEntry(Path.GetFileName(path), diag.Code, diag.Message, diag.Range);
                        Console.WriteLine(entry);
                        analysisResults.AddEntry(entry);
                        outputFile.WriteLine(diag.Code);
                        outputFile.WriteLine(diag.Message);
                        outputFile.WriteLine(diag.Range);
                        outputFile.WriteLine();
                    });
                }
            }

            return analysisResults;
        }

        public async Task Cleanup()
        {
            await Client.Shutdown();
        }

        public Func<Task> GetAssessmentCompletionTasks(string solutionName)
        {
            async Task CompletionTask()
            {
                Console.WriteLine("CompletionTask called....");
                //await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
             /*   try
                {
                    if (!UserSettings.Instance.EnabledContinuousAssessment)
                    {
                        UserSettings.Instance.EnabledContinuousAssessment = true;
                        UserSettings.Instance.UpdateContinuousAssessment();
                        await PortingAssistantLanguageClient.UpdateUserSettingsAsync();
                    }
                    await NotificationUtils.UseStatusBarProgressAsync(2, 2, "Assessment successful");
                    await NotificationUtils.ShowInfoBarAsync(this.package, "Assessment successful. You can view the assessment results in the error list or view the green highlights in your source code.");
                }
                catch (Exception ex)
                {
                    NotificationUtils.ShowErrorMessageBox(package, $"Assessment failed for {solutionName} due to {ex.Message}", "Assessment failed");
                }
                finally
                {
                    CommandsCommon.EnableAllCommand(true);
                } */
            }
            return CompletionTask;
        }
    }

    /*
    class PipeUtils
    {
        public static void StartListenerConnection(string pipeName, Func<Task> taskToRun)
        {
            System.Threading.Tasks.Task.Factory.StartNew(async () =>
            {
                NamedPipeServerStream server = null;
                try
                {
                    Console.WriteLine("StartListenerConnection start");
                    server = new NamedPipeServerStream(pipeName);

                    await server.WaitForConnectionAsync();
                    await ThreadHelper.JoinableTaskFactory.RunAsync(taskToRun);
                    

                    Console.WriteLine("StartListenerConnection end");

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
    */
}
