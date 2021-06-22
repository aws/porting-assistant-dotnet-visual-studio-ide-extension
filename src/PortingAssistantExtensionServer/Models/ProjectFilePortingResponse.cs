using System.Collections.Generic;

namespace PortingAssistantExtensionServer.Models
{
    public class ProjectFilePortingResponse
    {
        public bool Success { get; set; }
        public string SolutionPath { get; set; }
        public List<string> messages { get; set; }
    }
}
