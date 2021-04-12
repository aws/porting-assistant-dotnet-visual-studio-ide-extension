using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PortingAssistant.Client.Model;
using PortingAssistantExtension.Telemetry.Interface;
using PortingAssistantExtension.Telemetry.Model;
using System;
using System.IO;

namespace PortingAssistantExtension.Telemetry
{
    public class TelemetryCollector : ITelemetryCollector
    {
        private readonly ILogger _logger;
        private readonly string _filePath;

        public TelemetryCollector(ILogger<ITelemetryCollector> logger, string filePath)
        {
            _logger = logger;
            _filePath = filePath;
        }

        private void WriteToFile(string content)
        {
            try
            {
                lock (_filePath)
                {
                    using var file = new StreamWriter(_filePath, append: true);
                    file.WriteLine(content);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("write to metrics file failed", ex);
            }

        }

        public void SolutionAssessmentCollect(SolutionAnalysisResult result)
        {
            try
            {
                var date = DateTime.Now;
                var solutionDetail = result.SolutionDetails;
                // Solution Metrics
                var solutionMetrics = new SolutionMetrics
                {
                    MetricsType = MetricsType.solution,
                    PortingAssistantExtensionVersion = "1.0.0",
                    TargetFramework = "netcore3.0",
                    TimeStamp = date.ToString("MM/dd/yyyy HH:mm"),
                    SolutionPath = solutionDetail.SolutionFilePath,
                };
                WriteToFile(JsonConvert.SerializeObject(solutionMetrics));

                // Porject Metris = 
                foreach (var project in solutionDetail.Projects)
                {
                    var projectMetrics = new ProjectMetrics
                    {
                        MetricsType = MetricsType.solution,
                        PortingAssistantExtensionVersion = "1.0.0",
                        TargetFramework = "netcore3.0",
                        TimeStamp = date.ToString("MM/dd/yyyy HH:mm"),
                        projectGuid = project.ProjectGuid,
                        projectType = project.ProjectType,
                        numNugets = project.PackageReferences.Count,
                        numReferences = project.ProjectReferences.Count,
                        isBuildFailed = project.IsBuildFailed,
                    };
                    WriteToFile(JsonConvert.SerializeObject(projectMetrics));
                }

                //nuget metrics
                result.ProjectAnalysisResults.ForEach(project =>
                {
                    foreach (var nuget in project.PackageAnalysisResults)
                    {
                        nuget.Value.Wait();
                        var nugetMetrics = new NugetMetrics
                        {
                            MetricsType = MetricsType.solution,
                            PortingAssistantExtensionVersion = "1.0.0",
                            TargetFramework = "netcore3.0",
                            TimeStamp = date.ToString("MM/dd/yyyy HH:mm"),
                            pacakgeName = nuget.Value.Result.PackageVersionPair.PackageId,
                            packageVersion = nuget.Value.Result.PackageVersionPair.Version,
                            compatibility = nuget.Value.Result.CompatibilityResults["netcore3.0"].Compatibility,
                        };
                        WriteToFile(JsonConvert.SerializeObject(nugetMetrics));
                    }

                    foreach (var sourceFile in project.SourceFileAnalysisResults)
                    {
                        FileAssessmentCollect(sourceFile);
                    }
                });


            }
            catch (Exception ex)
            {
                _logger.LogError("Capture metrics failed with error", ex);
            }
        }

        public void FileAssessmentCollect(SourceFileAnalysisResult result)
        {

            var metrics = JsonConvert.SerializeObject(result);
            WriteToFile(metrics);
        }
    }
}
