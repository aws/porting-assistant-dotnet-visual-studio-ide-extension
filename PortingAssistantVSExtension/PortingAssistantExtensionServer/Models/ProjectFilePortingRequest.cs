using PortingAssistant.Client.Model;
using MediatR;
using System.Collections.Generic;

namespace PortingAssistantExtensionServer.Models
{
    class ProjectFilePortingRequest : PortingRequest, IRequest<ProjectFilePortingResponse>
    {
        public List<string> ProjectPaths { get; set; }
    }
}
