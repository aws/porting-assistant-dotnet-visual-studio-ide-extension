using PortingAssistant.Client.Model;
using PortingAssistantExtensionTelemetry.Model;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace PortingAssistantExtensionTelemetry
{
    public static class Collector
    {
        public static void SolutionAssessmentCollect(SolutionAnalysisResult result, string targetFramework, string extensionVersion, double time)
        {
            var sha256hash = SHA256.Create();
            var date = DateTime.Now;
            var solutionDetail = result.SolutionDetails;
            // Solution Metrics
            var solutionMetrics = new SolutionMetrics
            {
                MetricsType = MetricsType.solution,
                PortingAssistantExtensionVersion = extensionVersion,
                TargetFramework = targetFramework,
                TimeStamp = date.ToString("MM/dd/yyyy HH:mm"),
                SolutionPath = GetHash(sha256hash, solutionDetail.SolutionFilePath),
                AnalysisTime = time,
            };
            TelemetryCollector.Collect<SolutionMetrics>(solutionMetrics);

            result.ProjectAnalysisResults.ForEach(projectAnalysisResult => {
                if (projectAnalysisResult == null) 
                {
                    return;
                }
                var projectMetrics = new ProjectMetrics
                {
                    MetricsType = MetricsType.project,
                    PortingAssistantExtensionVersion = extensionVersion,
                    TargetFramework = targetFramework,
                    sourceFrameworks = projectAnalysisResult.TargetFrameworks,
                    TimeStamp = date.ToString("MM/dd/yyyy HH:mm"),
                    projectGuid = projectAnalysisResult.ProjectGuid,
                    projectType = projectAnalysisResult.ProjectType,
                    numNugets = projectAnalysisResult.PackageReferences.Count,
                    numReferences = projectAnalysisResult.ProjectReferences.Count,
                    isBuildFailed = projectAnalysisResult.IsBuildFailed,
                    compatibilityResult = projectAnalysisResult.ProjectCompatibilityResult
                };
                TelemetryCollector.Collect<ProjectMetrics>(projectMetrics);

            });

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

                var selectedApis = project.SourceFileAnalysisResults.SelectMany(s => s.ApiAnalysisResults);
                FileAssessmentCollect(selectedApis, targetFramework, extensionVersion);
                
            });
        }


        public static void FileAssessmentCollect(IEnumerable<ApiAnalysisResult> selectedApis , string targetFramework, string extensionVersion)
        {
            var date = DateTime.Now;
            var apiMetrics = selectedApis.GroupBy(elem => new
            {
                elem.CodeEntityDetails.Name,
                elem.CodeEntityDetails.Namespace,
                elem.CodeEntityDetails.OriginalDefinition,
                elem.CodeEntityDetails.Package?.PackageId,
                elem.CodeEntityDetails.Signature
            }).Select(group => new APIMetrics
            {
                MetricsType = MetricsType.api,
                PortingAssistantExtensionVersion = extensionVersion,
                TargetFramework = targetFramework,
                TimeStamp = date.ToString("MM/dd/yyyy HH:mm"),
                name = group.First().CodeEntityDetails.Name,
                nameSpace = group.First().CodeEntityDetails.Namespace,
                originalDefinition = group.First().CodeEntityDetails.OriginalDefinition,
                compatibility = group.First().CompatibilityResults[targetFramework].Compatibility,
                packageId = group.First().CodeEntityDetails.Package.PackageId,
                packageVersion = group.First().CodeEntityDetails.Package.Version,
                apiType = group.First().CodeEntityDetails.CodeEntityType.ToString(),
                hasActions = group.First().Recommendations.RecommendedActions.Any(action => action.RecommendedActionType != RecommendedActionType.NoRecommendation),
                apiCounts = group.Count()
            });
            apiMetrics.ToList().ForEach(metric => TelemetryCollector.Collect(metric));
        }

        private static string GetHash(HashAlgorithm hashAlgorithm, string input)
        {
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }
    }
}
