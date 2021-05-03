using MediatR;
using PortingAssistant.Client.Model;
using System.Collections.Generic;

namespace PortingAssistantExtensionServer.Models
{
    class ProjectFilePortingRequest : PortingRequest, IRequest<ProjectFilePortingResponse>
    {
        public List<string> ProjectPaths { get; set; }
        public string PipeName { get; set; }
        public override string ToString()
        {
            return $"ProjectPaths: {string.Join(", ", ProjectPaths)},  " +
                $"SolutionPath: {this.SolutionPath}, " +
                $"TargetFramework: {this.TargetFramework}, " +
                $"IncludeCodeFix: {this.IncludeCodeFix}";
        }
    }
}
