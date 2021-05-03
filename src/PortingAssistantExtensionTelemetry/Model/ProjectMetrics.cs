using System;
using System.Collections.Generic;
using System.Text;

namespace PortingAssistantExtensionTelemetry.Model
{
    public class ProjectMetrics : MetricsBase
    {
        public int numNugets { get; set; }
        public int numReferences { get; set; }
        public string projectGuid { get; set; }
        public bool isBuildFailed { get; set; }
        public string projectType { get; set; }
    }
}
