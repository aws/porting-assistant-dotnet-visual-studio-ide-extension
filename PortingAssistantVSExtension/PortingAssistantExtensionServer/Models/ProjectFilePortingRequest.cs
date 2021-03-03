using PortingAssistant.Client.Model;
using MediatR;
using System;
using System.Collections.Generic;

namespace PortingAssistantExtensionServer.Models
{
    class ProjectFilePortingRequest : IRequest<ProjectFilePortingResponse>
    {
        public List<string> ProjectPaths { get; set; }
        public string SolutionPath { get; set; }
        public string TargetFramework { get; set; }
        public List<PackageRecommendation> RecommendedActions { get; set; }
    }
}
