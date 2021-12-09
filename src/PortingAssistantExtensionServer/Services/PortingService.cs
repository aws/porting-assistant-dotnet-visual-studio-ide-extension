using Microsoft.Extensions.Logging;
using PortingAssistant.Client.Client;
using PortingAssistant.Client.Model;
using PortingAssistantExtensionServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PortingAssistantExtensionServer.Services
{
    class PortingService : BaseService, IDisposable
    {
        private readonly ILogger<PortingService> _logger;
        private readonly IPortingAssistantClient _client;
        public List<PackageAnalysisResult> PackageToAnalysisResults;
        public Dictionary<string, ProjectDetails> ProjectPathToDetails;

        public PortingService(ILogger<PortingService> logger,
    IPortingAssistantClient client)
        {
            _logger = logger;
            _client = client;
            PackageToAnalysisResults = new List<PackageAnalysisResult>();
            ProjectPathToDetails = new Dictionary<string, ProjectDetails>();
        }

        public async Task GetPackageAnalysisResultAsync(Task<SolutionAnalysisResult> SolutionAnalysisResultTask)
        {
            try
            {
                Task<SolutionAnalysisResult> solutionAnalysisResultTask = SolutionAnalysisResultTask;
                var solutionAnalysisResult = await solutionAnalysisResultTask;
                PackageToAnalysisResults = solutionAnalysisResult.ProjectAnalysisResults
                    .Where(project => project.PackageAnalysisResults != null)
                    .SelectMany(project => project.PackageAnalysisResults.Values
                    .Select(package => package?.Result)).ToList();
                ProjectPathToDetails = solutionAnalysisResult.ProjectAnalysisResults
                    .Where(projectAnalysisResult => !string.IsNullOrEmpty(projectAnalysisResult.ProjectFilePath))
                    .Select(p => new ProjectDetails()
                    {
                        ProjectName = p.ProjectName,
                        ProjectFilePath = p.ProjectFilePath,
                        ProjectGuid = p.ProjectGuid,
                        ProjectType = p.ProjectType,
                        TargetFrameworks = p.TargetFrameworks,
                        PackageReferences = p.PackageReferences,
                        ProjectReferences = p.ProjectReferences,
                        IsBuildFailed = p.IsBuildFailed
                    })
                    .ToDictionary(p => p.ProjectFilePath, p => p);
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
                if (ProjectPathToDetails == null || ProjectPathToDetails.Count == 0)
                {
                    return new ProjectFilePortingResponse()
                    {
                        Success = false,
                        messages = new List<string>() { "Please run a full assessment before porting" },
                        SolutionPath = request.SolutionPath
                    };
                }

                var portingRequst = new PortingRequest
                {

                    Projects = ProjectPathToDetails.Where(p => p.Value != null && request.ProjectPaths.Contains(p.Value.ProjectFilePath)).Select(p => p.Value).ToList(),
                    SolutionPath = request.SolutionPath,
                    RecommendedActions = GenerateRecommendedActions(request),
                    TargetFramework = request.TargetFramework,
                    IncludeCodeFix = request.IncludeCodeFix
                };
                _logger.LogInformation($"start porting ${request} .....");
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

        private List<RecommendedAction> GenerateRecommendedActions(ProjectFilePortingRequest request)
        {
            try
            {
                var upgradePackagesResults = PackageToAnalysisResults
        .Where(p =>
            p.CompatibilityResults.TryGetValue(request.TargetFramework, out var compatibilityResult)
            && compatibilityResult.Compatibility == Compatibility.INCOMPATIBLE
            && compatibilityResult.CompatibleVersions.Any()
            && compatibilityResult.CompatibleVersions.Exists(v => !v.Contains("-")));

                var packageToRecommendations = upgradePackagesResults.Select(package => new PackageRecommendation()
                {
                    PackageId = package.PackageVersionPair.PackageId,
                    Version = package.PackageVersionPair.Version,
                    TargetVersions = new List<string> { package.CompatibilityResults[request.TargetFramework].CompatibleVersions.First(v => !v.Contains("-")) },
                    RecommendedActionType = RecommendedActionType.UpgradePackage,
                });
                return packageToRecommendations.Select(r => (RecommendedAction)r).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "failed to generate recommended actions ");
                return null;
            }
        }
        public void Dispose()
        {
        }
    }
}
