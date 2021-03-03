using Microsoft.Extensions.Logging;
using PortingAssistant.Client.Client;
using PortingAssistant.Client.Model;
using PortingAssistantExtensionServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PortingAssistantExtensionServer
{
    class PortingService : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IPortingAssistantClient _client;

        public PortingService(ILogger<SolutionAnalysisService> logger,
    IPortingAssistantClient client)
        {
            _logger = logger;
            _client = client;
        }

        public ProjectFilePortingResponse  PortingProjects(ProjectFilePortingRequest request)
        {
            var portingRequst = new PortingRequest
            {
                ProjectPaths = request.ProjectPaths,
                SolutionPath = request.SolutionPath,
                RecommendedActions = new List<RecommendedAction>(),
                TargetFramework = request.TargetFramework
            };
            var results =  _client.ApplyPortingChanges(portingRequst);
            return new ProjectFilePortingResponse()
            {
                Success = results.All(r => r.Success),
                messages = results.Select(r => r.Message).ToList(),
                SolutionPath = request.SolutionPath
            };
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
