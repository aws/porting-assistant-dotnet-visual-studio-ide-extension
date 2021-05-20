using Microsoft.Extensions.Logging;
using PortingAssistant.Client.Model;
using PortingAssistantExtensionTelemetry.Model;
using System;

namespace PortingAssistantExtensionTelemetry
{
    public static class Collector
    {
        public static void SolutionAssessmentCollect(SolutionAnalysisResult result, string targetFramework, string extensionVersion, int time)
        {
            var date = DateTime.Now;
            var solutionDetail = result.SolutionDetails;
            // Solution Metrics
            var solutionMetrics = new SolutionMetrics
            {
                MetricsType = MetricsType.solution,
                PortingAssistantExtensionVersion = extensionVersion,
                TargetFramework = targetFramework,
                TimeStamp = date.ToString("MM/dd/yyyy HH:mm"),
                SolutionPath = solutionDetail.SolutionFilePath,
                AnalysisTime = time,
            };
            TelemetryCollector.Collect<SolutionMetrics>(solutionMetrics);

            foreach (var project in solutionDetail.Projects)
            {
                var projectMetrics = new ProjectMetrics
                {
                    MetricsType = MetricsType.project,
                    PortingAssistantExtensionVersion = extensionVersion,
                    TargetFramework = targetFramework,
                    TimeStamp = date.ToString("MM/dd/yyyy HH:mm"),
                    projectGuid = project.ProjectGuid,
                    projectType = project.ProjectType,
                    numNugets = project.PackageReferences.Count,
                    numReferences = project.ProjectReferences.Count,
                    isBuildFailed = project.IsBuildFailed,
                };
                TelemetryCollector.Collect<ProjectMetrics>(projectMetrics);
            }

            //nuget metrics
            result.ProjectAnalysisResults.ForEach(project =>
            {
                foreach (var nuget in project.PackageAnalysisResults)
                {
                    nuget.Value.Wait();
                    var nugetMetrics = new NugetMetrics
                    {
                        MetricsType = MetricsType.nuget,
                        PortingAssistantExtensionVersion = extensionVersion,
                        TargetFramework = targetFramework,
                        TimeStamp = date.ToString("MM/dd/yyyy HH:mm"),
                        pacakgeName = nuget.Value.Result.PackageVersionPair.PackageId,
                        packageVersion = nuget.Value.Result.PackageVersionPair.Version,
                        compatibility = nuget.Value.Result.CompatibilityResults[targetFramework].Compatibility,
                    };
                    TelemetryCollector.Collect<NugetMetrics>(nugetMetrics);
                }

                foreach (var sourceFile in project.SourceFileAnalysisResults)
                {
                    FileAssessmentCollect(sourceFile, targetFramework, extensionVersion);
                }
            });
        }


        public static void FileAssessmentCollect(SourceFileAnalysisResult result, string targetFramework, string extensionVersion)
        {
            var date = DateTime.Now;
            foreach (var api in result.ApiAnalysisResults)
            {
                var apiMetrics = new APIMetrics
                {
                    MetricsType = MetricsType.api,
                    PortingAssistantExtensionVersion = extensionVersion,
                    TargetFramework = targetFramework,
                    TimeStamp = date.ToString("MM/dd/yyyy HH:mm"),
                    name = api.CodeEntityDetails.Name,
                    nameSpace = api.CodeEntityDetails.Namespace,
                    originalDefinition = api.CodeEntityDetails.OriginalDefinition,
                    compatibility = api.CompatibilityResults[targetFramework].Compatibility,
                    packageId = api.CodeEntityDetails.Package.PackageId,
                    packageVersion = api.CodeEntityDetails.Package.Version
                };
                TelemetryCollector.Collect<APIMetrics>(apiMetrics);
            }
        }
    }
}
