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
using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using Codelyzer.Analysis.Model;

namespace PortingAssistantExtensionServer.Handlers
{
    using CompatibleItem = Dictionary<Compatibility, SortedSet<string>>;

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

        public class ProjectSerializeResult
        {
            public List<string> AllReferences = new List<string>();
            public CompatibleItem AllPackageResults { get; set; }
            public SortedSet<string> ReferencedProjects = new();
            public ExternalReferences ExternalReferences { get; set; }

            public SortedDictionary<string, CompatibleItem> AllAPIResults = new();

            public ProjectSerializeResult()
            {
                AllPackageResults = new()
                {
                    [Compatibility.COMPATIBLE] = new SortedSet<string>(),
                    [Compatibility.INCOMPATIBLE] = new SortedSet<string>(),
                    [Compatibility.UNKNOWN] = new SortedSet<string>(),
                    [Compatibility.DEPRECATED] = new SortedSet<string>(),
                };
                ExternalReferences = new();
            }
        };

        public class SolutionSerializeResult
        {
            public SortedSet<string> IncompatiblePackages = new SortedSet<string>();
            public SortedSet<string> CompatiblePackages = new SortedSet<string>();
            public SortedSet<string> UnknownPackages = new SortedSet<string>();
            public SortedSet<string> DeprecatedPackages = new SortedSet<string>();
            public SortedSet<string> TotalPackages = new SortedSet<string>();

            public SortedSet<string> IncompatibleApis = new SortedSet<string>();
            public SortedSet<string> CompatibleApis = new SortedSet<string>();
            public SortedSet<string> UnknownApis = new SortedSet<string>();
            public SortedSet<string> DeprecatedApis = new SortedSet<string>();
            public SortedSet<string> TotalApis = new SortedSet<string>();
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

            SolutionSerializeResult solutionResult = new();

            string rootPath = String.IsNullOrEmpty(request.workspaceConfig)
                ? @"D:\work\VSWorkspace\ResultBuildalyzer\"
                : @"D:\work\VSWorkspace\ResultVsWorkspace\";

            var solutioPath = Path.Combine(rootPath, Path.GetFileNameWithoutExtension(request.solutionFilePath));
            if (Directory.Exists(solutioPath))
            {
                Directory.Delete(solutioPath, true);
            }
            Directory.CreateDirectory(solutioPath);

            foreach (var project in solutionAnalysisResultResolved.ProjectAnalysisResults)
            {
                ProjectSerializeResult currentProjectObject = new();

                if (project.ProjectReferences != null)
                {
                    foreach (var reference in project.ProjectReferences)
                    {
                        currentProjectObject.ReferencedProjects.Add(reference.ReferencePath);
                    }
                }

                if (project.ExternalReferences.NugetDependencies.Count > 0)
                {
                    currentProjectObject.ExternalReferences.NugetDependencies = project.ExternalReferences.NugetDependencies
                        .OrderBy(s => s.Identity)
                        .ToList();
                }

                if (project.ExternalReferences.NugetReferences.Count > 0)
                {
                    currentProjectObject.ExternalReferences.NugetReferences = project.ExternalReferences.NugetReferences
                        .OrderBy(s => s.Identity)
                        .ToList();
                }

                if (project.ExternalReferences.SdkReferences.Count > 0)
                {
                    currentProjectObject.ExternalReferences.SdkReferences = project.ExternalReferences.SdkReferences
                        .OrderBy(s => s.Identity)
                        .ToList();
                }

                if (project.ExternalReferences.ProjectReferences.Count > 0)
                {
                    currentProjectObject.ExternalReferences.ProjectReferences = project.ExternalReferences.ProjectReferences
                        .OrderBy(s => s.Identity)
                        .ToList();
                }

                if (project.PackageAnalysisResults != null)
                {
                    foreach(var packageResult in project.PackageAnalysisResults)
                    {
                        var packageValueResult = packageResult.Value.Result;
                        currentProjectObject.AllPackageResults[packageValueResult.CompatibilityResults[request.settings.TargetFramework].Compatibility]
                            .Add(packageValueResult.PackageVersionPair.ToString());
                    }

                    var incompatiblePkg = project.PackageAnalysisResults
                        .Select(p => p.Value.Result)
                        .Where(package => package.CompatibilityResults[request.settings.TargetFramework].Compatibility == Compatibility.INCOMPATIBLE)
                        .Select(p => p.PackageVersionPair.ToString());
                    var comapatiblesPkg = project.PackageAnalysisResults
                        .Select(p => p.Value.Result)
                        .Where(package => package.CompatibilityResults[request.settings.TargetFramework].Compatibility == Compatibility.COMPATIBLE)
                        .Select(p => p.PackageVersionPair.ToString());
                    var unknownPkg = project.PackageAnalysisResults
                        .Select(p => p.Value.Result)
                        .Where(package => package.CompatibilityResults[request.settings.TargetFramework].Compatibility == Compatibility.UNKNOWN)
                        .Select(p => p.PackageVersionPair.ToString());
                    var deprecatedPkg = project.PackageAnalysisResults
                        .Select(p => p.Value.Result)
                        .Where(package => package.CompatibilityResults[request.settings.TargetFramework].Compatibility == Compatibility.DEPRECATED)
                        .Select(p => p.PackageVersionPair.ToString());
                    var totalPkg = project.PackageAnalysisResults.Select(p => p.Value.Result.PackageVersionPair.ToString());

                    solutionResult.CompatiblePackages.UnionWith(comapatiblesPkg);
                    solutionResult.IncompatiblePackages.UnionWith(incompatiblePkg);
                    solutionResult.UnknownPackages.UnionWith(unknownPkg);
                    solutionResult.DeprecatedPackages.UnionWith(deprecatedPkg);
                    solutionResult.TotalPackages.UnionWith(totalPkg);
                }

                if (project.SourceFileAnalysisResults != null)
                {
                    var projectOutputPath = Path.Combine(solutioPath, $"{project.ProjectName}.json");

                    foreach (var fileResult in project.SourceFileAnalysisResults)
                    {
                        CompatibleItem compatibleAPIs = new()
                        {
                            [Compatibility.COMPATIBLE] = new SortedSet<string>(),
                            [Compatibility.INCOMPATIBLE] = new SortedSet<string>(),
                            [Compatibility.UNKNOWN] = new SortedSet<string>(),
                            [Compatibility.DEPRECATED] = new SortedSet<string>()
                        };

                        foreach (var apiResult in fileResult.ApiAnalysisResults)
                        {
                            compatibleAPIs[apiResult.CompatibilityResults[request.settings.TargetFramework].Compatibility]
                                .Add(apiResult.CodeEntityDetails.Signature);
                        }

                        currentProjectObject.AllAPIResults[fileResult.SourceFilePath] = compatibleAPIs;
                    }

                    currentProjectObject.AllReferences = project.MetaReferences.OrderBy(v => v).ToList();
                    string allOutputString = JsonConvert.SerializeObject(currentProjectObject, Formatting.Indented);

                    await File.WriteAllTextAsync(
                        projectOutputPath,
                        allOutputString,
                        cancellationToken);

                    var apiResults = project.SourceFileAnalysisResults
                        .SelectMany(codeAnalyzeResult => codeAnalyzeResult?.ApiAnalysisResults ?? Enumerable.Empty<ApiAnalysisResult>());

                    var incompatibleApi = apiResults
                        .Where(api => api.CompatibilityResults[request.settings.TargetFramework].Compatibility == Compatibility.INCOMPATIBLE)
                        .Select(api => api.CodeEntityDetails.Signature);

                    var compatibleApi = apiResults
                        .Where(api => api.CompatibilityResults[request.settings.TargetFramework].Compatibility == Compatibility.COMPATIBLE)
                        .Select(api => api.CodeEntityDetails.Signature);

                    var unknownApi = apiResults
                        .Where(api => api.CompatibilityResults[request.settings.TargetFramework].Compatibility == Compatibility.UNKNOWN)
                        .Select(api => api.CodeEntityDetails.Signature);

                    var deprecatedApi = apiResults
                        .Where(api => api.CompatibilityResults[request.settings.TargetFramework].Compatibility == Compatibility.DEPRECATED)
                        .Select(api => api.CodeEntityDetails.Signature);

                    var totalApi = project.SourceFileAnalysisResults
                        .SelectMany(codeAnalyzeResult => codeAnalyzeResult?.ApiAnalysisResults?
                            .Select(api => api?.CodeEntityDetails?.Signature)
                            .Where(signature => !string.IsNullOrEmpty(signature))
                            ?? Enumerable.Empty<string>());

                    solutionResult.CompatibleApis.UnionWith(compatibleApi);
                    solutionResult.IncompatibleApis.UnionWith(incompatibleApi);
                    solutionResult.UnknownApis.UnionWith(unknownApi);
                    solutionResult.DeprecatedApis.UnionWith(deprecatedApi);
                    solutionResult.TotalApis.UnionWith(totalApi);
                }
            }

            string solutionOutputString = JsonConvert.SerializeObject(solutionResult, Formatting.Indented);
            string solutionOutputPath = Path.Combine(rootPath, Path.GetFileNameWithoutExtension(request.solutionFilePath) + ".json");
            await File.WriteAllTextAsync(
                solutionOutputPath,
                solutionOutputString,
                cancellationToken);

            Console.WriteLine(
                $"Total nuget packages: {solutionResult.TotalPackages.Count}, " +
                $"compatible: {solutionResult.CompatiblePackages.Count}, " +
                $"incompatibles: {solutionResult.IncompatiblePackages.Count}, " +
                $"unknown: {solutionResult.UnknownPackages.Count}, " +
                $"deprecated: {solutionResult.DeprecatedPackages.Count}");
            Console.WriteLine(
                $"Total nuget packages: {solutionResult.TotalApis.Count}, " +
                $"compatible: {solutionResult.CompatibleApis.Count}, " +
                $"incompatibles: {solutionResult.IncompatibleApis.Count}, " +
                $"unknown: {solutionResult.UnknownApis.Count}, " +
                $"deprecated: {solutionResult.DeprecatedApis.Count}");

            return new AnalyzeSolutionResponse()
            {
                incompatibleNugetPackages = solutionResult.IncompatiblePackages.Count,
                portableNugetPackages = solutionResult.CompatiblePackages.Count,
                totalNugetPackages = solutionResult.TotalPackages.Count,
                incompatibleAPis = solutionResult.IncompatibleApis.Count,
                portableAPis = solutionResult.CompatibleApis.Count,
                totalApis = solutionResult.TotalApis.Count,
                portableProjects = solutionAnalysisResultResolved.ProjectAnalysisResults?.Where(p => p.ProjectCompatibilityResult?.IsPorted ?? false).Count() ?? 0,
                totalProjects = solutionAnalysisResultResolved.SolutionDetails?.Projects?.Count() ?? 0,
                hasWebFormsProject = solutionAnalysisResultResolved.ProjectAnalysisResults?.Any(p => p.FeatureType == "WebForms") ?? false
            };
        }
    }
}
