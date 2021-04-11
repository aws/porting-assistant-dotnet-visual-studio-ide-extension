using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistantVSExtensionClient.Models
{
    public class AnalyzeSolutionRequest
    {
        public string solutionFilePath { get; set; }
        public AnalyzerSettings settings { get; set; }
        public bool ContiniousEnabled { get; set; }
    }
	public class AnalyzerSettings
	{
		public List<string> IgnoreProjects { get; set; }
		public string TargetFramework { get; set; }
	}
}
