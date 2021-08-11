using Microsoft.Extensions.Logging;
using PortingAssistant.Client.Client;
using PortingAssistant.Client.Model;
using PortingAssistantExtensionServer.Models;
using PortingAssistantExtensionServer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PortingAssistantExtensionServer
{
    class PortingService : BaseService, IDisposable
    {
        private readonly ILogger<PortingService> _logger;
        private readonly IPortingAssistantClient _client;
        public Dictionary<string, PackageAnalysisResult> PackageToAnalysisResults;

        public PortingService(ILogger<PortingService> logger,
    IPortingAssistantClient client)
        {
            _logger = logger;
            _client = client;
            PackageToAnalysisResults = new Dictionary<string, PackageAnalysisResult>();
        }

        public async Task GetPackageAnalysisResultAsync(Task<SolutionAnalysisResult> SolutionAnalysisResultTask)
        {
            try
            {
                PackageToAnalysisResults = new Dictionary<string, PackageAnalysisResult>();
                Task<SolutionAnalysisResult> solutionAnalysisResultTask = SolutionAnalysisResultTask;
                var solutionAnalysisResult = await solutionAnalysisResultTask;

                foreach (var projectAnalysisResult in solutionAnalysisResult.ProjectAnalysisResults)
                {
                    foreach (var packageAnalysisResultPair in projectAnalysisResult.PackageAnalysisResults)
                    {
                        var packageAnalysisResult = await packageAnalysisResultPair.Value;
                        PackageToAnalysisResults[packageAnalysisResultPair.Key.PackageId] = packageAnalysisResult;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "get diagnostics failed with error: ");
            }
        }

        public ProjectFilePortingResponse PortingProjects(ProjectFilePortingRequest request)
        {
            try
            {
                var compatiblePacakges = PackageToAnalysisResults
                    .Select(package => package.Value)
                    .Where(packageAnalysisResult => packageAnalysisResult.CompatibilityResults.TryGetValue(request.TargetFramework, out var compatibilityResult) && compatibilityResult.Compatibility == Compatibility.COMPATIBLE)
                    .ToList();

                var packageToRecommendations = compatiblePacakges.Select(package => new PackageRecommendation()
                {
                    PackageId = package.PackageVersionPair.PackageId,
                    Version = package.PackageVersionPair.Version,
                    TargetVersions = new List<string> { package.CompatibilityResults[request.TargetFramework].CompatibleVersions.FirstOrDefault() },
                    RecommendedActionType = RecommendedActionType.UpgradePackage,
                });
                var portingRequst = new PortingRequest
                {
                    Projects = request.ProjectPaths.Select(p => new ProjectDetails() { ProjectFilePath = p }).ToList(),
                    SolutionPath = request.SolutionPath,
                    RecommendedActions = packageToRecommendations.Select(r => (RecommendedAction)r).ToList(),
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
