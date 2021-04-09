using MediatR;
using PortingAssistant.Client.Model;
using System.Collections.Generic;

namespace PortingAssistantExtensionServer.Models
{
    class AnalyzeRequest : IRequest<AnalyzeResponse>
    {
        public string solutionFilePath { get; set; }
        public List<string> sourceFilePaths { get; set; }
        public AnalyzerSettings settings { get; set; }
    }
}
