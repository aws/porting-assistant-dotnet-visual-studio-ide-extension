using Microsoft.Extensions.Logging;
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

namespace PortingAssistantExtensionServer
{
    internal class SolutionAnalysisService : IDisposable
    {
        public SolutionAnalysisResult SolutionAnalysisResult;
        private readonly ILogger _logger;
        private readonly IPortingAssistantClient _client;

        public ImmutableDictionary<DocumentUri, CodeFileDocument> _openDocuments = ImmutableDictionary<DocumentUri, CodeFileDocument>.Empty.WithComparers(DocumentUri.Comparer);

        public SolutionAnalysisService(ILogger<SolutionAnalysisService> logger,
            IPortingAssistantClient client)
        {
            _logger = logger;
            _client = client;
        }

        public void SetSolutionAnalysisResult(Task<SolutionAnalysisResult> SolutionAnalysisResultTask)
        {
            SolutionAnalysisResultTask.Wait();
            Console.WriteLine(SolutionAnalysisResultTask.Result.ToString());
            this.SolutionAnalysisResult = SolutionAnalysisResultTask.Result;
        }

        public Task<SolutionAnalysisResult> AssessSolutionAsync(AnalyzeSolutionRequest request)
        {
            var result = _client.AnalyzeSolutionAsync(request.solutionFilePath, request.settings);
            SetSolutionAnalysisResult(result);
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
                Href = new Uri("https://aws.amazon.com/porting-assistant-dotnet/")
            };

            foreach (var i in result.ProjectAnalysisResults)
            {

                var apis = i.SourceFileAnalysisResults.Where(sf => TrimFilePath(sf.SourceFilePath) == TrimFilePath(fileUri.Path)).SelectMany(sf => sf.ApiAnalysisResults).ToList();
                if (apis == null || apis.Count == 0) continue;
                foreach (var j in apis)
                {
                    if (j.CompatibilityResults["netcoreapp3.1"].Compatibility == Compatibility.INCOMPATIBLE)
                    {
                        var name = j.CodeEntityDetails.OriginalDefinition;
                        var rcommnadation = j.Recommendations.RecommendedActions.Select(r => (r.RecommendedActionType + r.Description));
                        var message = name + String.Join(",", rcommnadation);
                        var span = j.CodeEntityDetails.TextSpan;
                        var range = new Range(
                            new Position((int)(span.StartLinePosition - 1), (int)(span.StartCharPosition - 1)),
                    new Position((int)(span.EndLinePosition - 1), (int)(span.EndCharPosition - 1)));
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
            }
            return diagnostics;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        private string TrimFilePath(string path)
        {
            return path.Trim().Replace("\\", "").Replace("/", "");
        }
    }
}
