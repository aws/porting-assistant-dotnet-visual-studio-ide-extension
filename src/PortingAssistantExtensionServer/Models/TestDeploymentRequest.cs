using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace PortingAssistantExtensionServer.Models
{
    public class TestDeploymentRequest : IRequest<TestDeploymentResponse>
    {
        public String fileName { get; set; }
        public List<String> arguments { get; set; }
        public string PipeName { get; set; }
    }
}
