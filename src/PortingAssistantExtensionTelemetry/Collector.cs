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
        public static void SolutionAssessmentCollect(
            SolutionAnalysisResult result, string runId, string triggerType,
            string targetFramework, string extensionVersion,
            string visualStudioVersion, double time,
            string visualStudioFullVersion,
            bool useDefaultCredentials)
        {
            var sha256hash = SHA256.Create();
            var date = DateTime.Now;
            var solutionDetail = result.SolutionDetails;
            // Solution Metrics
            var solutionMetrics = new SolutionMetrics
            {
                MetricsType = MetricsType.solution,
                RunId = runId,
                TriggerType = triggerType,
                PortingAssistantExtensionVersion = extensionVersion,
                VisualStudioClientVersion = visualStudioVersion,
                TargetFramework = targetFramework,
                TimeStamp = date.ToString("MM/dd/yyyy HH:mm"),
                SolutionPath = GetHash(sha256hash, solutionDetail.SolutionFilePath),
                ApplicationGuid = result.SolutionDetails.ApplicationGuid,
                SolutionGuid = result.SolutionDetails.SolutionGuid,
                RepositoryUrl = result.SolutionDetails.RepositoryUrl,
                AnalysisTime = time,
                VisualStudioClientFullVersion = visualStudioFullVersion,
                UsingDefaultCreds = useDefaultCredentials,
            };
            TelemetryCollector.Collect<SolutionMetrics>(solutionMetrics);

            result.ProjectAnalysisResults.ForEach(projectAnalysisResult => {
                if (projectAnalysisResult == null) 
                {
                    return;
                }
                var compatbilityResult = projectAnalysisResult.ProjectCompatibilityResult;
                compatbilityResult.ProjectPath = GetHash(sha256hash, compatbilityResult.ProjectPath);
                var projectMetrics = new ProjectMetrics
                {
                    MetricsType = MetricsType.project,
                    RunId = runId,
                    TriggerType = triggerType,
                    PortingAssistantExtensionVersion = extensionVersion,
                    VisualStudioClientVersion = visualStudioVersion,
                    TargetFramework = targetFramework,
                    sourceFrameworks = projectAnalysisResult.TargetFrameworks,
                    TimeStamp = date.ToString("MM/dd/yyyy HH:mm"),
                    projectGuid = projectAnalysisResult.ProjectGuid,
                    projectType = projectAnalysisResult.ProjectType,
                    numNugets = projectAnalysisResult.PackageReferences.Count,
                    numReferences = projectAnalysisResult.ProjectReferences.Count,
                    isBuildFailed = projectAnalysisResult.IsBuildFailed,
                    compatibilityResult = compatbilityResult,
                    VisualStudioClientFullVersion = visualStudioFullVersion,
                    projectLanguage = GetProjectLanguage(projectAnalysisResult.ProjectFilePath)
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
                        RunId = runId,
                        TriggerType = triggerType,
                        PortingAssistantExtensionVersion = extensionVersion,
                        VisualStudioClientVersion = visualStudioVersion,
                        TargetFramework = targetFramework,
                        TimeStamp = date.ToString("MM/dd/yyyy HH:mm"),
                        pacakgeName = nuget.Value.Result.PackageVersionPair.PackageId,
                        packageVersion = nuget.Value.Result.PackageVersionPair.Version,
                        compatibility = nuget.Value.Result.CompatibilityResults[targetFramework].Compatibility,
                        VisualStudioClientFullVersion = visualStudioFullVersion
                    };
                    TelemetryCollector.Collect<NugetMetrics>(nugetMetrics);
                }


                var allActions = project.SourceFileAnalysisResults.SelectMany(a => a.RecommendedActions);
                var selectedApis = project.SourceFileAnalysisResults.SelectMany(s => s.ApiAnalysisResults);

                allActions.ToList().ForEach(action => {
                    var selectedApi = selectedApis.FirstOrDefault(s => s.CodeEntityDetails.TextSpan.Equals(action.TextSpan));
                    selectedApi?.Recommendations?.RecommendedActions?.Add(action);
                });

                FileAssessmentCollect(selectedApis, runId, triggerType, targetFramework, extensionVersion, visualStudioVersion, visualStudioFullVersion);
            });
        }


        public static void FileAssessmentCollect(
            IEnumerable<ApiAnalysisResult> selectedApis, string runId,
            string triggerType, string targetFramework, string extensionVersion,
            string visualStudioVersion, string visualStudioFullVersion)
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
                RunId = runId,
                TriggerType = triggerType,
                PortingAssistantExtensionVersion = extensionVersion,
                VisualStudioClientVersion = visualStudioVersion,
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
                apiCounts = group.Count(),
                VisualStudioClientFullVersion = visualStudioFullVersion
            });
            apiMetrics.ToList().ForEach(metric => TelemetryCollector.Collect(metric));
        }

        public static void ContinuousAssessmentCollect(
            SourceFileAnalysisResult result, string runId, string triggerType,
            string targetFramework, string extensionVersion,
            string visualStudioVersion, int diagnostics,
            string visualStudioFullVersion)
        {
            var timeStamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm");

            TelemetryCollector.Collect<CodeFileMetrics>(new CodeFileMetrics()
            {
                FilePath = result.SourceFilePath,
                TimeStamp = timeStamp,
                MetricsType = MetricsType.codeFile,
                PortingAssistantExtensionVersion = extensionVersion,
                VisualStudioClientVersion = visualStudioVersion,
                TargetFramework = targetFramework,
                Diagnostics = diagnostics,
                RunId = runId,
                TriggerType = triggerType,
                VisualStudioClientFullVersion = visualStudioFullVersion
            });
        }

        public static void ActivationCollect(
            string extensionVersion,
            string visualStudioVersion, string visualStudioFullVersion)
        {
            var timeStamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm");

            TelemetryCollector.Collect<ActivationMetric>(new ActivationMetric()
            {

                TimeStamp = timeStamp,
                MetricsType = MetricsType.activation,
                PortingAssistantExtensionVersion = extensionVersion,
                VisualStudioClientVersion = visualStudioVersion,
                TargetFramework = "",
                RunId = "",
                TriggerType = "",
                VisualStudioClientFullVersion = visualStudioFullVersion
            });
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

        private static string GetProjectLanguage(string projectFilePath)
        {
            if (projectFilePath.EndsWith(".csproj"))
            {
                return "csharp";
            }
            if (projectFilePath.EndsWith(".vbproj"))
            {
                return "visualbasic";
            }
            return "invalid";
        }
    }
}
