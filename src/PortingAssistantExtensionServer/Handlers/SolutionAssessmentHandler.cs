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
using PortingAssistant.Client.Model;

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

            var projectAnalyzeResult = solutionAnalysisResultResolved.ProjectAnalysisResults;

            HashSet<PackageVersionPair> incompatiblePackages = new HashSet<PackageVersionPair>();
            HashSet<PackageVersionPair> portablePackages = new HashSet<PackageVersionPair>();
            HashSet<PackageVersionPair> totalPackages = new HashSet<PackageVersionPair>();

            HashSet<string> incompatibleApis = new HashSet<string>();
            HashSet<string> portableApis = new HashSet<string>();
            HashSet<string> totalApis = new HashSet<string>();

            foreach (var project in solutionAnalysisResultResolved.ProjectAnalysisResults)
            {
                if (project.PackageAnalysisResults != null)
                {
                    var portablesPkg = project.PackageAnalysisResults
                        .Select(p => p.Value.Result)
                        .Where(package => package.CompatibilityResults[request.settings.TargetFramework].Compatibility == Compatibility.INCOMPATIBLE)
                        .Select(p => p.PackageVersionPair);
                    var incomapatiblesPkg = project.PackageAnalysisResults
                        .Select(p => p.Value.Result)
                        .Where(package => package.CompatibilityResults[request.settings.TargetFramework].Compatibility != Compatibility.COMPATIBLE)
                        .Select(p => p.PackageVersionPair);
                    var totalPkg = project.PackageAnalysisResults.Select(p => p.Value.Result.PackageVersionPair);

                    portablePackages.UnionWith(portablesPkg);
                    incompatiblePackages.UnionWith(incomapatiblesPkg);
                    totalPackages.UnionWith(totalPkg);
                }

                if (project.SourceFileAnalysisResults != null)
                {
                    var apiResults = project.SourceFileAnalysisResults
                        .SelectMany(codeAnalyzeResult => codeAnalyzeResult?.ApiAnalysisResults ?? Enumerable.Empty<ApiAnalysisResult>());

                    var portableApi = apiResults
                        .Where(api => api.CompatibilityResults[request.settings.TargetFramework].Compatibility == Compatibility.INCOMPATIBLE)
                        .Select(api => api.CodeEntityDetails.Signature);

                    var incompatibleApi = apiResults
                        .Where(api => api.CompatibilityResults[request.settings.TargetFramework].Compatibility != Compatibility.COMPATIBLE)
                        .Select(api => api.CodeEntityDetails.Signature);

                    var totalApi = project.SourceFileAnalysisResults
                        .SelectMany(codeAnalyzeResult => codeAnalyzeResult?.ApiAnalysisResults?
                            .Select(api => api?.CodeEntityDetails?.Signature)
                            .Where(signature => !string.IsNullOrEmpty(signature))
                            ?? Enumerable.Empty<string>());

                    portableApis.UnionWith(portableApi);
                    incompatibleApis.UnionWith(incompatibleApi);
                    totalApis.UnionWith(totalApi);
                }
            }

            return new AnalyzeSolutionResponse()
            {
                incompatibleNugetPackages = incompatiblePackages.Count,
                portableNugetPackages = portablePackages.Count,
                totalNugetPackages = totalPackages.Count,
                incompatibleAPis = incompatibleApis.Count,
                portableAPis = portableApis.Count,
                totalApis = totalApis.Count,
                portableProjects = solutionAnalysisResultResolved.ProjectAnalysisResults?.Where(p => p.ProjectCompatibilityResult?.IsPorted ?? false).Count() ?? 0,
                totalProjects = solutionAnalysisResultResolved.SolutionDetails?.Projects?.Count() ?? 0,
                hasWebFormsProject = solutionAnalysisResultResolved.ProjectAnalysisResults?.Any(p => p.FeatureType == "WebForms") ?? false
            };
        }
    }
}
