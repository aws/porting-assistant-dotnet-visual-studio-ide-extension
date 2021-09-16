using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistantVSExtensionClient.Models
{
    class ProjectFilePortingResponse
    {
        public bool NeedAssessment { get; set; }
        public bool Success { get; set; }
        public string SolutionPath { get; set; }
        public List<string> messages { get; set; }
    }
}
