using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistantVSExtensionClient.Models
{
    public class DeploymentParameters
    {
        public string profileName { set; get; }
        public bool enableMetrics { set; get; }
        public bool initDeploymentTool { set; get; }
        public string buildFolderPath { set; get; }
        public string deployname { get; set; }
        public string vpcId { set; get; }
        public string directoryId { set; get; }
        public string domainSecretsArn { set; get; }

        public string domainDNSName { set; get; }
        public string domainNetBIOSName { set; get; }
        public string servicePrincipalName { set; get; }
        public bool enablePasswordRotation { set; get; }
    }
}
