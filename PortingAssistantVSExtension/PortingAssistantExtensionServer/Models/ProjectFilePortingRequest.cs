using PortingAssistant.Client.Model;
using MediatR;

namespace PortingAssistantExtensionServer.Models
{
    class ProjectFilePortingRequest : PortingRequest, IRequest<ProjectFilePortingResponse>
    {
        public bool InludeCodeFix { get; set; }
    }
}
