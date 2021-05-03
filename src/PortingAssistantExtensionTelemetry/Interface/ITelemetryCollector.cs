using Microsoft.Extensions.Logging;
using PortingAssistant.Client.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace PortingAssistantExtensionTelemetry.Interface
{
    public interface ITelemetryCollector
    {
        public void SolutionAssessmentCollect(SolutionAnalysisResult result, string targetFramework, string extensionVersion, int time);
        public void FileAssessmentCollect(SourceFileAnalysisResult result, string targetFramework, string extensionVersion);
    }
}
