using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace PortingAssistantExtensionServer.Models
{
    public class TestDeploymentRequest : IRequest<TestDeploymentResponse>
    {
        public string fileName { get; set; }
        public List<string> arguments { get; set; }
    }
}
