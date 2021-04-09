﻿using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using PortingAssistant.Client.Client;
using PortingAssistant.Client.Model;
using PortingAssistantExtensionServer.Models;
using PortingAssistantExtensionServer.TextDocumentModels;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using PortingAssistantExtensionServer.Models;
using PortingAssistant.Client.Client;
using OmniSharp.Extensions.LanguageServer.Protocol;
using System.Collections.Immutable;
using PortingAssistantExtensionServer.TextDocumentModels;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace PortingAssistantExtensionServer
{
    internal class SolutionAnalysisService : IDisposable
    {
        public SolutionAnalysisResult SolutionAnalysisResult;
        private AnalyzeSolutionRequest _request;
        private readonly ILogger _logger;
        private readonly IPortingAssistantClient _client;

        public ImmutableDictionary<DocumentUri, CodeFileDocument> _openDocuments = ImmutableDictionary<DocumentUri, CodeFileDocument>.Empty.WithComparers(DocumentUri.Comparer);

        public SolutionAnalysisService(ILogger<SolutionAnalysisService> logger,
            IPortingAssistantClient client)
        {
            _logger = logger;
            _client = client;
        }

        public async Task SetSolutionAnalysisResultAsync(Task<SolutionAnalysisResult> SolutionAnalysisResultTask)
        {
            Task<SolutionAnalysisResult> solutionAnalysisResultTask = SolutionAnalysisResultTask;
            SolutionAnalysisResult = await solutionAnalysisResultTask;
            Console.WriteLine(SolutionAnalysisResult.ToString());
        }

        public async Task AssessSolutionAsync(AnalyzeSolutionRequest request)
        {
            _request = request;
            var result = _client.AnalyzeSolutionAsync(request.solutionFilePath, request.settings);
            await SetSolutionAnalysisResultAsync(result);
        }

        public async Task AssessFileAsync(List<string> filePaths)
        {
            var result = await _client.AnalyzeFileAsync(
                filePaths,
                _request.solutionFilePath,
                SolutionAnalysisResult.AnalyzerResults,
                SolutionAnalysisResult.ProjectActions,
                _request.settings);

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
                var targetSourceFiles = SolutionAnalysisResult.ProjectAnalysisResults
                    .Select(p => p.SourceFileAnalysisResults.Find(f => f.SourceFilePath == sourceFileAnalysisResult.SourceFilePath));
                foreach (var targetFile in targetSourceFiles)
                {
                    var target = targetFile;
                    target = sourceFileAnalysisResult;
                }
            });
        }

        private string TrimFilePath(string path)
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
