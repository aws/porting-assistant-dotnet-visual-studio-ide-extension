﻿using System;
using System.Collections.Generic;
using System.Text;
using PortingAssistant.Client.Common.Model;

namespace PortingAssistantExtensionTelemetry.Model
{
    public class ProjectMetrics : MetricsBase
    {
        public int numNugets { get; set; }
        public int numReferences { get; set; }
        public string projectGuid { get; set; }
        public bool isBuildFailed { get; set; }
        public string projectType { get; set; }
        public List<String> sourceFrameworks { get; set; }
        public ProjectCompatibilityResult compatibilityResult { get; set; }
        public string projectLanguage { get; set;  }
    }
}
