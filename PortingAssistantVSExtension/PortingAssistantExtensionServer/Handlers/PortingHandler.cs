using OmniSharp.Extensions.JsonRpc;
using PortingAssistantExtensionServer.Models;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace PortingAssistantExtensionServer.Handlers
{
    [Serial, Method("applyPortingProjectFileChanges")]

    internal interface ISolutionPortingHandler : IJsonRpcRequestHandler<ProjectFilePortingRequest, ProjectFilePortingResponse> { }
    internal class PortingHandler : ISolutionPortingHandler
    {
        private readonly ILogger _logger;
        private readonly PortingService _portingService;
        public PortingHandler(ILogger<PortingHandler> logger,
            PortingService portingService)
        {
            _logger = logger;
            _portingService = portingService;
        }

        public Task<ProjectFilePortingResponse> Handle(ProjectFilePortingRequest request, CancellationToken cancellationToken)
        {
            return Task.Run(() => _portingService.PortingProjects(request));
        }
    }
}
