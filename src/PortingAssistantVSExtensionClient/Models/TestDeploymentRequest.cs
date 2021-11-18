using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistantVSExtensionClient.Models
{
    public class TestDeploymentRequest
    {
        public string excutionType { get; set; }
        public string command { get; set; }
        public List<string> arguments
        {
            get; set;
        }
    }
}
