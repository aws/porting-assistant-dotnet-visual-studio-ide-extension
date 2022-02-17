using CTA.Rules.Models;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using PortingAssistant.Client.Client;
using PortingAssistant.Client.Model;
using PortingAssistantExtensionTelemetry.Interface;
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
using PortingAssistantExtensionServer.Services;

[assembly: InternalsVisibleTo("PortingAssistantExtensionUnitTest")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
namespace PortingAssistantExtensionServer
{
    internal class AnalysisService : BaseService, IDisposable
    {
        private AnalyzeSolutionRequest _request;
        private readonly ILogger<AnalysisService> _logger;
        private readonly IPortingAssistantClient _client;
        private readonly ITelemetryCollector _telemetry;
        public Dictionary<int, IList<TextChange>> CodeActions;
        public Dictionary<DocumentUri, ProjectAnalysisResult> FileToProjectAnalyssiResult;

        public ImmutableDictionary<DocumentUri, CodeFileDocument> _openDocuments = ImmutableDictionary<DocumentUri, CodeFileDocument>.Empty.WithComparers(DocumentUri.Comparer);

        public AnalysisService(
            ILogger<AnalysisService> logger,
            IPortingAssistantClient client,
            ITelemetryCollector telemetry)
        {
            _logger = logger;
            _client = client;
            _telemetry = telemetry;
            CodeActions = new Dictionary<int, IList<TextChange>>();
            FileToProjectAnalyssiResult = new Dictionary<DocumentUri, ProjectAnalysisResult>();
        }


        public async Task<SolutionAnalysisResult> AssessSolutionAsync(AnalyzeSolutionRequest request)
        {
            try
            {

                // Clean up the existing result before run full assessment
                Cleanup();
                _request = request;
                var startTime = DateTime.Now.Millisecond;
                var solutionAnalysisResult = await _client.AnalyzeSolutionAsync(request.solutionFilePath, request.settings);
                _telemetry.SolutionAssessmentCollect(
                    solutionAnalysisResult,
                    _request.settings.TargetFramework,
                    PALanguageServerConfiguration.ExtensionVersion,
                    DateTime.Now.Millisecond - startTime);
                CreateClientConnectionAsync(request.PipeName);
                return solutionAnalysisResult;
            }
            catch (Exception e)
            {
                _logger.LogError($"Analyze solution {request.solutionFilePath} with error ", e);
                return new SolutionAnalysisResult
                {
                    ProjectAnalysisResults = new List<ProjectAnalysisResult> {
                        new ProjectAnalysisResult
                        {
                            PreportMetaReferences = new List<string>(),
                            MetaReferences = new List<string>(),
                            ExternalReferences = new ExternalReferences(),
                            ProjectRules = new RootNodes()
                        }
                    },
                };
            }
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

                foreach (var sourceFileAnalysisResult in result)
                {
                    _telemetry.FileAssessmentCollect(
                        sourceFileAnalysisResult,
                        _request.settings.TargetFramework,
                        PALanguageServerConfiguration.ExtensionVersion);
                }

                return result;
            }
            catch (Exception e)
            {
                _logger.LogError("incremental assessment failed with error: ", e);
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

                    foreach (var sourceFileAnalysisResult in projectAnalysisResult.SourceFileAnalysisResults)
                    {
                        var sourceFileUri = DocumentUri.FromFileSystemPath(sourceFileAnalysisResult.SourceFilePath);
                        var diagnostics = GetDiagnostics(sourceFileAnalysisResult);
                        if (!FileToFirstDiagnostics.ContainsKey(sourceFileUri))
                        {
                            FileToFirstDiagnostics.Add(sourceFileUri, diagnostics);
                        }
                    }
                }

                return FileToFirstDiagnostics;
            }
            catch (Exception e)
            {
                _logger.LogError("get diagnostics failed with error ", e);
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
                                        return "Replace API with " + r.Description;
                                    case RecommendedActionType.ReplaceNamespace:
                                        return "Replace namespace with " + r.Description;
                                    case RecommendedActionType.NoRecommendation:
                                        break;
                                    default:
                                        break;
                                }
                                return "";
                            });
                        var message = $"Porting Assistant: {name} is incompatible for target framework {_request.settings.TargetFramework} " + String.Join(", ", rcommnadation);
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
                    catch (Exception e)
                    {
                        _logger.LogError("Add API diagnostic failed with error ", e);
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
                    _logger.LogError("Add porting action diagnostic failed with error", ex);
                }
            }

            return diagnostics;
        }


        public async Task<IList<Diagnostic>> GetDiagnosticsAsync(DocumentUri fileUri)
        {
            _openDocuments.TryGetValue(fileUri, out var document);
            var result = await AssessFileAsync(document, false);
            var sourceFileAnalysisResult = result.FirstOrDefault();
            return GetDiagnostics(sourceFileAnalysisResult);
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
