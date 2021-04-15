using CTA.Rules.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using PortingAssistant.Client.Client;
using PortingAssistant.Client.Model;
using PortingAssistantExtension.Telemetry.Interface;
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
        private readonly ILogger _logger;
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
            _telemetry.SolutionAssessmentCollect(SolutionAnalysisResult);
        }

        public async Task AssessSolutionAsync(AnalyzeSolutionRequest request)
        {
            _request = request;
            var result = _client.AnalyzeSolutionAsync(request.solutionFilePath, request.settings);
            await SetSolutionAnalysisResultAsync(result);
        }

        public async Task AssessFileAsync(CodeFileDocument codeFile)
        {
            var projectFile = codeFile.GetProjectFile();
            var projectAnalysisResult = SolutionAnalysisResult.ProjectAnalysisResults.FirstOrDefault(p => p.ProjectFilePath == projectFile);

            var result = await _client.AnalyzeFileAsync(codeFile.NormalizedPath, projectFile, _request.solutionFilePath,
                projectAnalysisResult.PreportMetaReferences, projectAnalysisResult.MetaReferences, projectAnalysisResult.ProjectRules, projectAnalysisResult.ExternalReferences, _request.settings);
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
                Href = new Uri("https://aws.amazon.com/porting-assistant-dotnet/")
            };
            foreach (var projectAnalysisResult in result.ProjectAnalysisResults)
            {
                var sourceFileAnalysisResults = projectAnalysisResult.SourceFileAnalysisResults
                    .Where(sf => TrimFilePath(sf.SourceFilePath) == TrimFilePath(fileUri.Path));

                var apis = sourceFileAnalysisResults.SelectMany(sf => sf.ApiAnalysisResults).ToList();
                var recommendedActionsList = sourceFileAnalysisResults.Select(sf => sf.RecommendedActions).ToList();

                foreach (var api in apis)
                {
                    //TODO Change hardcoded values
                    if (api.CompatibilityResults["netcoreapp3.1"].Compatibility == Compatibility.INCOMPATIBLE)
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
                        var diagnositc = new Diagnostic()
                        {
                            Severity = DiagnosticSeverity.Warning,
                            Code = new DiagnosticCode("pa-test01"),
                            Source = "Porting Assistant",
                            CodeDescription = codedescrption,
                            Tags = new Container<DiagnosticTag>(new List<DiagnosticTag>() { DiagnosticTag.Deprecated }),
                            Range = range,
                            RelatedInformation = new Container<DiagnosticRelatedInformation>(new List<DiagnosticRelatedInformation>() { new DiagnosticRelatedInformation() {
                                Location = location,
                                Message = "related message"
                            } }),
                            Message = message,
                            Data = JToken.Parse("{Data:\"Test\"}", new JsonLoadSettings() { })
                        };
                        diagnostics.Add(diagnositc);
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
                            var data = recommendedAction.TextChanges != null ? JToken.Parse(JsonConvert.SerializeObject(recommendedAction.TextChanges.ToList())) : null;
                            var diagnositc = new Diagnostic()
                            {
                                Severity = DiagnosticSeverity.Warning,
                                Code = new DiagnosticCode("pa-test01"),
                                Source = "Porting Assistant",
                                CodeDescription = codedescrption,
                                Tags = new Container<DiagnosticTag>(new List<DiagnosticTag>() { DiagnosticTag.Deprecated }),
                                Range = range,
                                RelatedInformation = new Container<DiagnosticRelatedInformation>(new List<DiagnosticRelatedInformation>() {new DiagnosticRelatedInformation(){
                                Location = location,
                                Message = "related message"
                            } }),
                                Message = recommendedAction.Description,
                                Data = data
                            };
                            diagnostics.Add(diagnositc);
                            var hashDiagnostics = HashDiagnostic(diagnositc, fileUri.Path);
                            if (CodeActions.ContainsKey(hashDiagnostics))
                            {
                                CodeActions.Remove(hashDiagnostics);
                            }
                            CodeActions.Add(HashDiagnostic(diagnositc, fileUri.Path), recommendedAction.TextChanges);
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

        public int HashDiagnostic(Diagnostic diagnostic, string documentPath) => HashCode.Combine(diagnostic.Message, diagnostic.Range, documentPath);


        public void Dispose()
        {
            throw new NotImplementedException();
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
                _telemetry.FileAssessmentCollect(sourceFileAnalysisResult);
            });
        }

        public string TrimFilePath(string path)
        {
            return path.Trim().Replace("\\", "").Replace("/", "");
        }

        private Range GetRange(TextSpan span)
        {
            return new Range(
                new Position((int)(span.StartLinePosition - 1), (int)(span.StartCharPosition - 1)),
                new Position((int)(span.EndLinePosition - 1), (int)(span.EndCharPosition - 1)));
        }
    }
}
