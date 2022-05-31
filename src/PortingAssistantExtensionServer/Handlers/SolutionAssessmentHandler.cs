using OmniSharp.Extensions.JsonRpc;
using PortingAssistantExtensionServer.Models;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using System.Linq;
using System.Collections.Generic;
using PortingAssistantExtensionServer.Services;

namespace PortingAssistantExtensionServer.Handlers
{
    [Serial, Method("analyzeSolution")]
    internal interface ISolutionAssessmentHandler : IJsonRpcRequestHandler<AnalyzeSolutionRequest, AnalyzeSolutionResponse> { }
    internal class SolutionAssessmentHandler : ISolutionAssessmentHandler
    {
        private readonly ILogger<ISolutionAssessmentHandler> _logger;
        private readonly AnalysisService _analysisService;
        private readonly PortingService _portingService;
        private readonly ILanguageServerFacade _languageServer;
        public SolutionAssessmentHandler(ILogger<SolutionAssessmentHandler> logger,
            ILanguageServerFacade languageServer,
            AnalysisService analysisService,
            PortingService portingService
            )
        {
            _logger = logger;
            _languageServer = languageServer;
            _analysisService = analysisService;
            _portingService = portingService;
        }

        public async Task<AnalyzeSolutionResponse> Handle(AnalyzeSolutionRequest request, CancellationToken cancellationToken)
        {
            var solutionAnalysisResult = _analysisService.AssessSolutionAsync(request);
            var diagnostics = await _analysisService.GetDiagnosticsAsync(solutionAnalysisResult);
            await _portingService.GetPackageAnalysisResultAsync(solutionAnalysisResult);

            foreach (var diagnostic in diagnostics)
            {
                IList<Diagnostic> diag = new List<Diagnostic>();
                if (_analysisService._openDocuments.ContainsKey(diagnostic.Key))
                {
                    diag = diagnostic.Value;
                }

                if (!_analysisService._openDocuments.ContainsKey(diagnostic.Key) && diagnostic.Value.Count != 0)
                {
                    diag.Add(diagnostic.Value.FirstOrDefault());
                }

                _languageServer.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams()
                {
                    Diagnostics = new Container<Diagnostic>(diag),
                    Uri = diagnostic.Key,
                });
            }

            var solutionAnalysisResultResolved = await solutionAnalysisResult.ConfigureAwait(true);

            foreach(var p in solutionAnalysisResultResolved.ProjectAnalysisResults)
            {
                _logger.LogInformation("Feature Type: " + p.FeatureType);
            }

            var hasWebForms = solutionAnalysisResultResolved.ProjectAnalysisResults.Any(p => p.FeatureType == "WebForms");

            return new AnalyzeSolutionResponse()
            {
                incompatibleAPis = 1,
                incompatiblePacakges = 1,
                hasWebFormsProject = solutionAnalysisResultResolved.ProjectAnalysisResults.Any(p => p.FeatureType == "WebForms")
            };
        }
    }
}
