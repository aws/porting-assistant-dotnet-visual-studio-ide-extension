using MediatR;
using OmniSharp.Extensions.JsonRpc;
using PortingAssistant.Client.Client;
using PortingAssistantExtensionServer.Models;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using PortingAssistant.Client.Model;

namespace PortingAssistantExtensionServer
{
    [Serial, Method("analyzeSolution")]
    internal interface IAssessmentSolutionHandler : IJsonRpcRequestHandler<AnalyzeSolutionRequest, SolutionAnalysisResult> { }
    internal class AssessmentSolutionHandler : IAssessmentSolutionHandler
    {
        private readonly ILogger _logger;
        private readonly IPortingAssistantClient _client;
        public AssessmentSolutionHandler(ILogger<AssessmentSolutionHandler> logger,
            IPortingAssistantClient client)
        {
            _logger = logger;
            _client = client;
        }

        public  Task<SolutionAnalysisResult> Handle(AnalyzeSolutionRequest request, CancellationToken cancellationToken)
        {
            return  _client.AnalyzeSolutionAsync(request.solutionFilePath, request.settings);
        }
    }
}
