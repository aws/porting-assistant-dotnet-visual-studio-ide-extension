using CTA.Rules.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using PortingAssistant.Client.Client;
using PortingAssistant.Client.Model;
using PortingAssistantExtension.Telemetry.Interface;
using PortingAssistantExtensionServer.Common;
using PortingAssistantExtensionServer.Models;
using PortingAssistantExtensionServer.TextDocumentModels;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace PortingAssistantExtensionServer
{
    internal class SolutionAnalysisService : IDisposable
    {
        public SolutionAnalysisResult SolutionAnalysisResult;
        private AnalyzeSolutionRequest _request;
        private readonly ILogger<SolutionAnalysisService> _logger;
        private readonly IPortingAssistantClient _client;
        private readonly ITelemetryCollector _telemetry;
        public Dictionary<int, IList<TextChange>> CodeActions;

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
        }

        public async Task SetSolutionAnalysisResultAsync(Task<SolutionAnalysisResult> SolutionAnalysisResultTask)
        {
            Task<SolutionAnalysisResult> solutionAnalysisResultTask = SolutionAnalysisResultTask;
            SolutionAnalysisResult = await solutionAnalysisResultTask;
            _telemetry.SolutionAssessmentCollect(
                SolutionAnalysisResult, 
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
            var projectAnalysisResult = SolutionAnalysisResult.ProjectAnalysisResults.FirstOrDefault(p => p.ProjectFilePath == projectFile);

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
            return SolutionAnalysisResult != null;
        }

        public SolutionAnalysisResult GetSolutionAnalysisResult()
        {
            return SolutionAnalysisResult;
        }


        public List<Diagnostic> GetDiagnostics(DocumentUri fileUri)
        {
            var diagnostics = new List<Diagnostic>();
            if (!HasSolutionAnalysisResult()) return diagnostics;
            var result = GetSolutionAnalysisResult();

            var codedescrption = new CodeDescription()
            {
                //TODO Move to a constants class
                Href = new Uri(Constant.PortingAssitantHelpUrl)
            };
            foreach (var projectAnalysisResult in result.ProjectAnalysisResults)
            {
                var sourceFileAnalysisResults = projectAnalysisResult.SourceFileAnalysisResults
                    .Where(sf => TrimFilePath(sf.SourceFilePath) == TrimFilePath(fileUri.Path));

                var apis = sourceFileAnalysisResults.SelectMany(sf => sf.ApiAnalysisResults).ToList();
                var recommendedActionsList = sourceFileAnalysisResults.Select(sf => sf.RecommendedActions).ToList();

                foreach (var api in apis)
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

                foreach (var recommendedActions in recommendedActionsList)
                {
                    foreach (var recommendedAction in recommendedActions)
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
                }
            }
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
            SolutionAnalysisResult = null;
            _request = null;
        }

        private void UpdateSolutionAnalysisResult(IncrementalFileAnalysisResult analysisResult)
        {

            SolutionAnalysisResult.AnalyzerResults = analysisResult.analyzerResults;
            SolutionAnalysisResult.ProjectActions = analysisResult.projectActions;



            analysisResult.sourceFileAnalysisResults.ForEach(sourceFileAnalysisResult =>
            {
                var projectResult = SolutionAnalysisResult.ProjectAnalysisResults
                    .First(p => p.SourceFileAnalysisResults.Any(f => TrimFilePath(f.SourceFilePath) == TrimFilePath(sourceFileAnalysisResult.SourceFilePath)));

                var oldFile = projectResult.SourceFileAnalysisResults.FirstOrDefault(s => TrimFilePath(s.SourceFilePath) == TrimFilePath(sourceFileAnalysisResult.SourceFilePath));

                projectResult.SourceFileAnalysisResults.Remove(oldFile);
                projectResult.SourceFileAnalysisResults.Add(sourceFileAnalysisResult);
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
