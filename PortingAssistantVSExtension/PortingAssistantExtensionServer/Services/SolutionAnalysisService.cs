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

namespace PortingAssistantExtensionServer
{
    internal class SolutionAnalysisService : IDisposable
    {
        private AnalyzeSolutionRequest _request;
        private readonly ILogger<SolutionAnalysisService> _logger;
        private readonly IPortingAssistantClient _client;
        private readonly ITelemetryCollector _telemetry;
        public Dictionary<int, IList<TextChange>> CodeActions;
        public Dictionary<string, IList<Diagnostic>> FileToDiagnostics;
        public Dictionary<string, ProjectAnalysisResult> FileToProjectAnalyssiResult;

        public ImmutableDictionary<DocumentUri, CodeFileDocument> _openDocuments = ImmutableDictionary<DocumentUri, CodeFileDocument>.Empty.WithComparers(DocumentUri.Comparer);

        public SolutionAnalysisService(
            ILogger<SolutionAnalysisService> logger,
            IPortingAssistantClient client,
            ITelemetryCollector telemetry)
        {
            _logger = logger;
            _client = client;
            _telemetry = telemetry;
            CodeActions = new Dictionary<int, IList<TextChange>>();
            FileToDiagnostics = new Dictionary<string, IList<Diagnostic>>();
            FileToProjectAnalyssiResult = new Dictionary<string, ProjectAnalysisResult>();
        }

        public async Task SetSolutionAnalysisResultAsync(Task<SolutionAnalysisResult> SolutionAnalysisResultTask)
        {
            Task<SolutionAnalysisResult> solutionAnalysisResultTask = SolutionAnalysisResultTask;
            var solutionAnalysisResult = await solutionAnalysisResultTask;

            foreach (var projectAnalysisResult in solutionAnalysisResult.ProjectAnalysisResults)
            {
                if (!FileToProjectAnalyssiResult.ContainsKey(projectAnalysisResult.ProjectFilePath))
                {
                    FileToProjectAnalyssiResult.Add(projectAnalysisResult.ProjectFilePath, new ProjectAnalysisResult
                    {
                        PreportMetaReferences = projectAnalysisResult.PreportMetaReferences,
                        MetaReferences = projectAnalysisResult.MetaReferences,
                        ExternalReferences = projectAnalysisResult.ExternalReferences,
                        ProjectRules = projectAnalysisResult.ProjectRules
                    });
                }
                foreach (var sourceFileAnalysisResult in projectAnalysisResult.SourceFileAnalysisResults)
                {
                    UpdateDiagnostics(sourceFileAnalysisResult);
                }

            }

            _telemetry.SolutionAssessmentCollect(
            solutionAnalysisResult,
            _request.settings.TargetFramework,
            PALanguageServerConfiguration.ExtensionVersion);
        }

        public async Task AssessSolutionAsync(AnalyzeSolutionRequest request)
        {
            _request = request;
            var result = _client.AnalyzeSolutionAsync(request.solutionFilePath, request.settings);
            await SetSolutionAnalysisResultAsync(result);
        }

        public async Task<IncrementalFileAnalysisResult> AssessFileAsync(CodeFileDocument codeFile, bool actionsOnly = false)
        {
            var projectFile = codeFile.GetProjectFile();
            //var projectAnalysisResult = SolutionAnalysisResult.ProjectAnalysisResults.FirstOrDefault(p => p.ProjectFilePath == projectFile);
            var projectAnalysisResult = FileToProjectAnalyssiResult.GetValueOrDefault(projectFile, new ProjectAnalysisResult
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

            if (!result.sourceFileAnalysisResults.Any())
            {
                result.sourceFileAnalysisResults.Add(new SourceFileAnalysisResult()
                {
                    SourceFilePath = codeFile.NormalizedPath,
                    RecommendedActions = new List<RecommendedAction>(),
                    ApiAnalysisResults = new List<ApiAnalysisResult>(),
                    SourceFileName = Path.GetFileNameWithoutExtension(codeFile.NormalizedPath)
                });
            }

            UpdateSolutionAnalysisResult(result);

            return result;
        }

        public bool HasSolutionAnalysisResult()
        {
            return FileToDiagnostics.Count != 0;
        }

        public bool UpdateDiagnostics(SourceFileAnalysisResult sourceFileAnalysisResult)
        {
            var diagnostics = new List<Diagnostic>();
            var sourceFilePath = TrimFilePath(sourceFileAnalysisResult.SourceFilePath);
            var fileUri = DocumentUri.FromFileSystemPath(sourceFileAnalysisResult.SourceFilePath);
            var codedescrption = new CodeDescription()
            {
                //TODO Move to a constants class
                Href = new Uri(Constant.PortingAssitantHelpUrl)
            };

            foreach (var api in sourceFileAnalysisResult.ApiAnalysisResults)
            {
                if (api.CompatibilityResults[_request.settings.TargetFramework].Compatibility == Compatibility.INCOMPATIBLE)
                {
                    var name = api.CodeEntityDetails.OriginalDefinition;
                    var rcommnadation = api.Recommendations.RecommendedActions.Select(r => (r.RecommendedActionType + r.Description));
                    var message = name + String.Join(",", rcommnadation);
                    var range = GetRange(api.CodeEntityDetails.TextSpan);
                    var location = new Location()
                    {
                        Uri = fileUri,
                        Range = range
                    };
                    var diagnostic = new Diagnostic()
                    {
                        Severity = DiagnosticSeverity.Warning,
                        Code = new DiagnosticCode(Constant.DiagnosticCode),
                        Source = Constant.DiagnosticSource,
                        CodeDescription = codedescrption,
                        Range = range,
                        RelatedInformation = new Container<DiagnosticRelatedInformation>(new List<DiagnosticRelatedInformation>() { new DiagnosticRelatedInformation() {
                                Location = location
                            } }),
                        Message = message,
                    };
                    diagnostics.Add(diagnostic);
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
                        Code = new DiagnosticCode(Constant.DiagnosticWithActionCode),
                        Source = Constant.DiagnosticSource,
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
                    _logger.LogError("failed with error", ex);
                }
            }

            if (FileToDiagnostics.ContainsKey(sourceFilePath))
            {
                FileToDiagnostics.Remove(sourceFilePath);
            }

            return FileToDiagnostics.TryAdd(sourceFilePath, diagnostics);
        }


        public IList<Diagnostic> GetDiagnostics(DocumentUri fileUri)
        {
            return FileToDiagnostics.GetValueOrDefault(TrimFilePath(fileUri.Path), new List<Diagnostic>());
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
            FileToDiagnostics = null;
            FileToProjectAnalyssiResult = null;
            CodeActions = null;
        }

        public void UpdateSolutionAnalysisResult(IncrementalFileAnalysisResult analysisResult)
        {
            analysisResult.sourceFileAnalysisResults.ForEach(sourceFileAnalysisResult =>
            {
                UpdateDiagnostics(sourceFileAnalysisResult);
                _telemetry.FileAssessmentCollect(
                    sourceFileAnalysisResult,
                    _request.settings.TargetFramework,
                    PALanguageServerConfiguration.ExtensionVersion);
            });
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
