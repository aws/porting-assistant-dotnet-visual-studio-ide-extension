using MediatR;
using OmniSharp.Extensions.JsonRpc;
using PortingAssistantExtensionServer.Models;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using PortingAssistant.Client.Model;
using System;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;

namespace PortingAssistantExtensionServer.Handlers
{
    [Serial, Method("analyzeSolution")]
    internal interface ISolutionAssessmentHandler : IJsonRpcRequestHandler<AnalyzeSolutionRequest, AnalyzeSolutionResponse> { }
    internal class SolutionAssessmentHandler : ISolutionAssessmentHandler
    {
        private readonly ILogger _logger;
        private readonly SolutionAnalysisService _analysisService;
        private readonly ILanguageServerFacade _languageServer;
        public SolutionAssessmentHandler(ILogger<SolutionAssessmentHandler> logger,
            ILanguageServerFacade languageServer,
            SolutionAnalysisService analysisService)
        {
            _logger = logger;
            _languageServer = languageServer;
            _analysisService = analysisService;
            
        }

        public async Task<AnalyzeSolutionResponse> Handle(AnalyzeSolutionRequest request, CancellationToken cancellationToken)
        {
            await _analysisService.AssessSolutionAsync(request);
            var diagnostics = new List<Diagnostic>();
            foreach (var doc in _analysisService._openDocuments.Keys)
            {
                _languageServer.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams()
                {
                    Diagnostics = new Container<Diagnostic>(_analysisService.GetDiagnostics(doc)),
                    Uri = doc,
                });
            }

            return new AnalyzeSolutionResponse()
            {
                incompatibleAPis = 1,
                incompatiblePacakges = 1
            };
        }
    }
}
