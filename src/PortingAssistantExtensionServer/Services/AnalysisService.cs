using CTA.Rules.Models;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using PortingAssistant.Client.Client;
using PortingAssistant.Client.Model;
using PortingAssistantExtensionServer.Common;
using PortingAssistantExtensionServer.Models;
using PortingAssistantExtensionServer.TextDocumentModels;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TextSpan = PortingAssistant.Client.Model.TextSpan;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using Codelyzer.Analysis.Model;
using Constants = PortingAssistantExtensionServer.Common.Constants;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using PortingAssistantExtensionTelemetry;

[assembly: InternalsVisibleTo("PortingAssistantExtensionUnitTest")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
namespace PortingAssistantExtensionServer.Services
{
    internal class AnalysisService : BaseService, IDisposable
    {
        private AnalyzeSolutionRequest _request;
        private readonly ILogger<AnalysisService> _logger;
        private readonly IPortingAssistantClient _client;
        public Dictionary<int, IList<TextChange>> CodeActions;
        public Dictionary<DocumentUri, ProjectAnalysisResult> FileToProjectAnalyssiResult;

        public ImmutableDictionary<DocumentUri, CodeFileDocument> _openDocuments = ImmutableDictionary<DocumentUri, CodeFileDocument>.Empty.WithComparers(DocumentUri.Comparer);
        public string runId { get; private set; }

        public AnalysisService(
            ILogger<AnalysisService> logger,
            IPortingAssistantClient client)
        {
            _logger = logger;
            _client = client;
            CodeActions = new Dictionary<int, IList<TextChange>>();
            FileToProjectAnalyssiResult = new Dictionary<DocumentUri, ProjectAnalysisResult>();
        }


        public async Task<SolutionAnalysisResult> AssessSolutionAsync(AnalyzeSolutionRequest request)
        {
            runId = Guid.NewGuid().ToString();
            var triggerType = "InitialRequest";
            var solutionAnalysisResult = new SolutionAnalysisResult();
            bool assessmentCompleted = false;
            var startTime = DateTime.Now;

            try
            {

                // Clean up the existing result before run full assessment
                Cleanup();
                _request = request;

                //solutionAnalysisResult = await _client.AnalyzeSolutionAsync(
                //    request.solutionFilePath,
                //    request.settings);

                solutionAnalysisResult = await _client.AnalyzeSolutionAsyncUsingVSWorkspace(
                    request.solutionFilePath,
                    request.settings,
                    request.workspaceConfig);

                assessmentCompleted = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Analyze solution {request.solutionFilePath} with error: ");
            }

            var totalMilliseconds = DateTime.Now.Subtract(startTime).TotalMilliseconds;
            _logger.LogDebug($"Total assessment time is: {totalMilliseconds} seconds.");

            if (assessmentCompleted)
            {
                try
                {
                    Collector.SolutionAssessmentCollect(
                        solutionAnalysisResult,
                        runId,
                        triggerType,
                        _request.settings.TargetFramework,
                        PALanguageServerConfiguration.ExtensionVersion,
                        PALanguageServerConfiguration.VisualStudioVersion,
                        DateTime.Now.Subtract(startTime).TotalMilliseconds,
                        PALanguageServerConfiguration.VisualStudioFullVersion,
                        PALanguageServerConfiguration.EnabledDefaultCredentials);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Analyze solution {request.solutionFilePath} succeeded, but failed at Collector.SolutionAssessmentCollect.");
                }
            }

            CreateClientConnectionAsync(request.PipeName);
            return solutionAnalysisResult;
        }

        public async Task<List<SourceFileAnalysisResult>> AssessFileAsync(CodeFileDocument codeFile, bool actionsOnly = false)
        {
            try
            {
                var projectFile = codeFile.GetProjectFile();
                var fileUri = DocumentUri.FromFileSystemPath(projectFile);
                var projectAnalysisResult = FileToProjectAnalyssiResult.GetValueOrDefault(fileUri, new ProjectAnalysisResult
                {
                    PreportMetaReferences = new List<string>(),
                    MetaReferences = new List<string>(),
                    ExternalReferences = new ExternalReferences(),
                    ProjectRules = new RootNodes()
                });

                _request.settings.ActionsOnly = actionsOnly;

                var result = await _client.AnalyzeFileAsync(codeFile.NormalizedPath, codeFile.GetText(), projectFile, _request.solutionFilePath,
                    projectAnalysisResult.PreportMetaReferences, projectAnalysisResult.MetaReferences, projectAnalysisResult.ProjectRules, projectAnalysisResult.ExternalReferences,
                    _request.settings);

                if (result.Count() == 0)
                {
                    result.Add(new SourceFileAnalysisResult()
                    {
                        SourceFilePath = codeFile.NormalizedPath,
                        RecommendedActions = new List<RecommendedAction>(),
                        ApiAnalysisResults = new List<ApiAnalysisResult>(),
                        SourceFileName = Path.GetFileNameWithoutExtension(codeFile.NormalizedPath)
                    });
                }

                var triggerType = "ContinuousAssessmentRequest";
                var allActions = result.SelectMany(a => a.RecommendedActions);
                var selectedApis = result.SelectMany(s => s.ApiAnalysisResults);

                allActions.ToList().ForEach(action =>
                {
                    var selectedApi = selectedApis.FirstOrDefault(s => s.CodeEntityDetails.TextSpan.Equals(action.TextSpan));
                    selectedApi?.Recommendations?.RecommendedActions?.Add(action);
                });

                Collector.FileAssessmentCollect(
                    selectedApis,
                    runId,
                    triggerType,
                    _request.settings.TargetFramework,
                    PALanguageServerConfiguration.ExtensionVersion,
                    PALanguageServerConfiguration.VisualStudioVersion,
                    PALanguageServerConfiguration.VisualStudioFullVersion
                    );
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "incremental assessment failed with error: ");
                return new List<SourceFileAnalysisResult>
                    {
                        new SourceFileAnalysisResult
                        {
                            SourceFilePath = codeFile.NormalizedPath,
                            RecommendedActions = new List<RecommendedAction>(),
                            ApiAnalysisResults = new List<ApiAnalysisResult>(),
                            SourceFileName = Path.GetFileNameWithoutExtension(codeFile.NormalizedPath)
                        }
                    };
            }
        }

        public bool HasSolutionAnalysisResult()
        {
            return FileToProjectAnalyssiResult.Count != 0;
        }

        public async Task<Dictionary<DocumentUri, IList<Diagnostic>>> GetDiagnosticsAsync(Task<SolutionAnalysisResult> SolutionAnalysisResultTask)
        {
            try
            {
                var FileToFirstDiagnostics = new Dictionary<DocumentUri, IList<Diagnostic>>();
                Task<SolutionAnalysisResult> solutionAnalysisResultTask = SolutionAnalysisResultTask;
                var solutionAnalysisResult = await solutionAnalysisResultTask;

                foreach (var projectAnalysisResult in solutionAnalysisResult.ProjectAnalysisResults)
                {
                    if (string.IsNullOrEmpty(projectAnalysisResult.ProjectFilePath))
                    {
                        // Very likely AssessSolutionAsync has encountered exception and returned empty SolutionAnalysisResult.
                        continue;
                    }
                    var projectFileUri = DocumentUri.FromFileSystemPath(projectAnalysisResult.ProjectFilePath);
                    if (projectFileUri != null && !FileToProjectAnalyssiResult.ContainsKey(projectFileUri))
                    {
                        FileToProjectAnalyssiResult.Add(projectFileUri, new ProjectAnalysisResult
                        {
                            PreportMetaReferences = projectAnalysisResult.PreportMetaReferences,
                            MetaReferences = projectAnalysisResult.MetaReferences,
                            ExternalReferences = projectAnalysisResult.ExternalReferences,
                            ProjectRules = projectAnalysisResult.ProjectRules
                        });
                    }

                    int numberOfExceptions = 0;
                    foreach (var sourceFileAnalysisResult in projectAnalysisResult.SourceFileAnalysisResults)
                    {
                        if (string.IsNullOrEmpty(sourceFileAnalysisResult.SourceFilePath))
                        {
                            continue;
                        }
                        try
                        {
                            var sourceFileUri = DocumentUri.FromFileSystemPath(sourceFileAnalysisResult.SourceFilePath);
                            if (sourceFileUri != null && !FileToFirstDiagnostics.ContainsKey(sourceFileUri))
                            {
                                var diagnostics = GetDiagnostics(sourceFileAnalysisResult);
                                FileToFirstDiagnostics.Add(sourceFileUri, diagnostics);
                            }
                        }
                        catch (Exception ex)
                        {
                            numberOfExceptions++;
                            _logger.LogError(ex, string.Format("Get diagnostic failed {0} times with error: ", numberOfExceptions));
                            if (numberOfExceptions > Constants.maxNumberOfGetDiagnosticExceptions)
                            {
                                throw new Exception("Get diagnostic exceeded maximum number of exceptions allowed");
                            }
                        }
                    }
                }

                return FileToFirstDiagnostics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "get diagnostics failed with error: ");
                return new Dictionary<DocumentUri, IList<Diagnostic>>();
            }
        }

        public IList<Diagnostic> GetDiagnostics(SourceFileAnalysisResult sourceFileAnalysisResult)
        {
            var diagnostics = new List<Diagnostic>();
            var fileUri = DocumentUri.FromFileSystemPath(sourceFileAnalysisResult.SourceFilePath);
            var codedescrption = new CodeDescription()
            {
                //TODO Move to a constants class
                Href = new Uri(Constants.PortingAssitantHelpUrl)
            };

            foreach (var api in sourceFileAnalysisResult.ApiAnalysisResults)
            {
                if (api.CompatibilityResults[_request.settings.TargetFramework].Compatibility == Compatibility.INCOMPATIBLE)
                {
                    try
                    {
                        var name = api.CodeEntityDetails.Signature;
                        var package = api.CodeEntityDetails.Package;
                        var rcommnadation = api.Recommendations.RecommendedActions
                            .Where(r => r.RecommendedActionType != RecommendedActionType.NoRecommendation)
                            .Select(r =>
                            {
                                switch (r.RecommendedActionType)
                                {
                                    case RecommendedActionType.UpgradePackage:
                                        return $"Upgrade Source Package { package.PackageId}-{package.Version} to version " + r.Description;
                                    case RecommendedActionType.ReplacePackage:
                                        return $"Replace Source Package { package.PackageId}-{package.Version} with " + r.Description;
                                    case RecommendedActionType.ReplaceApi:
                                        return r.Description;
                                    case RecommendedActionType.ReplaceNamespace:
                                        return r.Description;
                                    case RecommendedActionType.NoRecommendation:
                                        break;
                                    default:
                                        break;
                                }
                                return "";
                            });
                        var message = $"Porting Assistant: {name} is incompatible for target framework {_request.settings.TargetFramework} " + string.Join(", ", rcommnadation);
                        var range = GetRange(api.CodeEntityDetails.TextSpan);
                        var location = new Location()
                        {
                            Uri = fileUri,
                            Range = range
                        };
                        var diagnostic = new Diagnostic()
                        {
                            Severity = DiagnosticSeverity.Warning,
                            Code = new DiagnosticCode(Constants.DiagnosticCode),
                            Source = Constants.DiagnosticSource,
                            CodeDescription = codedescrption,
                            Range = range,
                            RelatedInformation = new Container<DiagnosticRelatedInformation>(new List<DiagnosticRelatedInformation>() { new DiagnosticRelatedInformation() {
                                Location = location
                            } }),
                            Message = message,
                        };
                        diagnostics.Add(diagnostic);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Add API diagnostic failed with error: ");
                    }
                }
            }

            foreach (var recommendedAction in sourceFileAnalysisResult.RecommendedActions)
            {
                var range = GetRange(recommendedAction.TextSpan);
                var location = new Location
                {
                    Uri = fileUri,
                    Range = range
                };
                try
                {
                    var diagnositc = new Diagnostic()
                    {
                        Severity = DiagnosticSeverity.Warning,
                        Code = new DiagnosticCode(Constants.DiagnosticWithActionCode),
                        Source = Constants.DiagnosticSource,
                        CodeDescription = codedescrption,
                        Range = range,
                        RelatedInformation = new Container<DiagnosticRelatedInformation>(new List<DiagnosticRelatedInformation>() {new DiagnosticRelatedInformation(){
                                Location = location
                            } }),
                        Message = recommendedAction.Description,
                    };
                    diagnostics.Add(diagnositc);
                    UpdateCodeAction(diagnositc.Message, diagnositc.Range, fileUri.Path, recommendedAction.TextChanges);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Add porting action diagnostic failed with error: ");
                }
            }

            return diagnostics;
        }


        public async Task<IList<Diagnostic>> GetDiagnosticsAsync(DocumentUri fileUri)
        {
            _openDocuments.TryGetValue(fileUri, out var document);
            if (document == null) return null;
            var triggerType = "ContinuousAssessmentRequest";
            var result = await AssessFileAsync(document, false);
            var sourceFileAnalysisResult = result.FirstOrDefault();
            var diagnostics = GetDiagnostics(sourceFileAnalysisResult);

            Collector.ContinuousAssessmentCollect(
                sourceFileAnalysisResult,
                runId,
                triggerType,
                _request.settings.TargetFramework,
                PALanguageServerConfiguration.ExtensionVersion,
                PALanguageServerConfiguration.VisualStudioVersion,
                diagnostics.Count,
                PALanguageServerConfiguration.VisualStudioFullVersion);

            return diagnostics;
        }

        public void UpdateCodeAction(string message, Range range, string documentPath, IList<TextChange> textChanges)
        {
            var hashDiagnostic = HashDiagnostic(message, range, documentPath);

            if (CodeActions.ContainsKey(hashDiagnostic))
            {
                CodeActions.Remove(hashDiagnostic);
            }
            CodeActions.Add(hashDiagnostic, textChanges);
        }

        public int HashDiagnostic(string message, Range range, string documentPath) => HashCode.Combine(message, range, documentPath);


        public void Dispose()
        {
            _request = null;
            FileToProjectAnalyssiResult = null;
            CodeActions = null;
        }

        public void Cleanup()
        {
            FileToProjectAnalyssiResult = new Dictionary<DocumentUri, ProjectAnalysisResult>();
            CodeActions = new Dictionary<int, IList<TextChange>>();
        }

        public string TrimFilePath(string path)
        {
            return path.Trim().Replace("\\", "").Replace("/", "");
        }

        public Range GetRange(TextSpan span)
        {
            return new Range(
                new Position((int)(span.StartLinePosition - 1), (int)(span.StartCharPosition - 1)),
                new Position((int)(span.EndLinePosition - 1), (int)(span.EndCharPosition - 1)));
        }
    }
}
