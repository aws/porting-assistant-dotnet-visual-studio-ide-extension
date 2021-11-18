using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace PortingAssistantExtensionServer.Models
{
    public class TestDeploymentRequest : IRequest<TestDeploymentResponse>
    {
        public string excutionType { get; set; }
        public string command { get; set; }
        public List<string> arguments { get; set; }
    }
}
