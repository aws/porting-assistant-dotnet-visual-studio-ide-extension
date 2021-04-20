using MediatR;
using PortingAssistant.Client.Model;

namespace PortingAssistantExtensionServer.Models
{
    class AnalyzeSolutionRequest : IRequest<AnalyzeSolutionResponse>
    {
        public string solutionFilePath { get; set; }
        public AnalyzerSettings settings { get; set; }
        public override string ToString()
        {
            return $"SolutionPath: {this.solutionFilePath}, " +
                $"TargetFramework: {this.settings.TargetFramework}, " +
                $"ContiniousEnabled: {this.settings.ContiniousEnabled}";
        }
    }
}
