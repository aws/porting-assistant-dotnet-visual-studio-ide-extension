using MediatR;
using PortingAssistant.Client.Model;
using System.Collections.Generic;

namespace PortingAssistantExtensionServer.Models
{
    class AnalyzeSolutionRequest : IRequest<AnalyzeSolutionResponse>
    {
        public string solutionFilePath { get; set; }
        public AnalyzerSettings settings { get; set; }
    }
}
