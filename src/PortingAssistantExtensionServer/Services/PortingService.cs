using Microsoft.Extensions.Logging;
using PortingAssistant.Client.Client;
using PortingAssistant.Client.Model;
using PortingAssistantExtensionServer.Models;
using PortingAssistantExtensionServer.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PortingAssistantExtensionServer
{
    class PortingService : BaseService, IDisposable
    {
        private readonly ILogger<PortingService> _logger;
        private readonly IPortingAssistantClient _client;

        public PortingService(ILogger<PortingService> logger,
    IPortingAssistantClient client)
        {
            _logger = logger;
            _client = client;
        }

        public ProjectFilePortingResponse PortingProjects(ProjectFilePortingRequest request)
        {
            try
            {
                var portingRequst = new PortingRequest
                {
                    Projects = request.ProjectPaths.Select(p => new ProjectDetails() { ProjectFilePath = p }).ToList(),
                    SolutionPath = request.SolutionPath,
                    RecommendedActions = new List<RecommendedAction>(),
                    TargetFramework = request.TargetFramework,
                    IncludeCodeFix = request.IncludeCodeFix
                };
                _logger.LogInformation($"start porting ${request.SolutionPath} .....");
                var results = _client.ApplyPortingChanges(portingRequst);
                CreateClientConnectionAsync(request.PipeName);
                _logger.LogInformation($"porting success ${request.SolutionPath}");
                return new ProjectFilePortingResponse()
                {
                    Success = results.All(r => r.Success),
                    messages = results.Select(r => r.Message).ToList(),
                    SolutionPath = request.SolutionPath
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "failed to port projects: ");
                return new ProjectFilePortingResponse()
                {
                    Success = false,
                    messages = new List<string>() { ex.Message },
                    SolutionPath = request.SolutionPath
                };
            }
        }
        public void Dispose()
        {
        }
    }
}
