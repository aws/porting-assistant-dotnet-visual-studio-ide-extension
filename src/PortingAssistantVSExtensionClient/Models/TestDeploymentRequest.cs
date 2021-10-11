using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistantVSExtensionClient.Models
{
    public class TestDeploymentRequest
    {
        public string fileName { get; set; }
        public List<string> arguments { get; set; }
    }
}
