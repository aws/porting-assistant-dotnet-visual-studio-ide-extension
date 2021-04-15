using Microsoft.Extensions.Logging;
using PortingAssistant.Client.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace PortingAssistantExtension.Telemetry.Interface
{
    public interface ITelemetryCollector
    {
        public void SolutionAssessmentCollect(SolutionAnalysisResult result, string targetFramework);
        public void FileAssessmentCollect(SourceFileAnalysisResult result, string targetFramework);
    }
}
