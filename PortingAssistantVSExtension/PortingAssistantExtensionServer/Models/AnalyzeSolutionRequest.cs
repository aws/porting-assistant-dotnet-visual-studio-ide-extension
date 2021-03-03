using MediatR;
using PortingAssistant.Client.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace PortingAssistantExtensionServer.Models
{
    class AnalyzeSolutionRequest : IRequest<SolutionAnalysisResult>
    {
        public string solutionFilePath { get; set; }
        public AnalyzerSettings settings { get; set; }
    }
}
