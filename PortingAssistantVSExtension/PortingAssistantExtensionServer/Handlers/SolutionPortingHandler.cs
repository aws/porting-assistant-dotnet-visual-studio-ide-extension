using MediatR;
using OmniSharp.Extensions.JsonRpc;
using PortingAssistant.Client.Client;
using PortingAssistantExtensionServer.Models;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using PortingAssistant.Client.Model;
using System;
using System.Collections.Generic;

namespace PortingAssistantExtensionServer.Handlers
{
    [Serial, Method("applyPortingProjectFileChanges")]

    internal interface ISolutionPortingHandler : IJsonRpcRequestHandler<ProjectFilePortingRequest, ProjectFilePortingResponse> { }
    internal class SolutionPortingHandler : ISolutionPortingHandler 
    {
        private readonly ILogger _logger;
        private readonly PortingService _portingService;
        public SolutionPortingHandler(ILogger<SolutionPortingHandler> logger,
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
