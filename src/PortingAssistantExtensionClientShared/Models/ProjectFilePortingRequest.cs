﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistantVSExtensionClient.Models
{
    public class ProjectFilePortingRequest
    {
        public List<string> ProjectPaths { get; set; }
        public string SolutionPath { get; set; }
        public string TargetFramework { get; set; }
        public string PipeName { get; set; }
        public bool IncludeCodeFix { get; set; }
    }
}
