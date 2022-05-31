using MediatR;
using PortingAssistant.Client.Model;
using System.Collections.Generic;

namespace PortingAssistantExtensionServer.Models
{
    public class AnalyzeSolutionRequest : IRequest<AnalyzeSolutionResponse>
    {
        public string solutionFilePath { get; set; }
        public Dictionary<string, List<string>> metaReferences { get; set; }
        public AnalyzerSettings settings { get; set; }
        public string PipeName { get; set; }
        public override string ToString()
        {
            return $"SolutionPath: {this.solutionFilePath}, " +
                $"TargetFramework: {this.settings.TargetFramework}, " +
                $"ContiniousEnabled: {this.settings.ContiniousEnabled}";
        }
    }
}
