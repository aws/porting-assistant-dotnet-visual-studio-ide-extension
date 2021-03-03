using System;
using System.Collections.Generic;
using System.Text;

namespace PortingAssistantExtensionServer.Models
{
    class ProjectFilePortingResponse
    {
        public bool Success { get; set; }
        public string SolutionPath { get; set; }

        public List<string> messages { get; set; }
    }
}
