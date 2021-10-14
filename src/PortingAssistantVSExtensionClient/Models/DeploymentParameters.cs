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
        public string BuildFolderPath { set; get; }
        public string DirectoryId { set; get; }
        public string DomainSecretsArn { set; get; }

        public string DomainDNSName { set; get; }
        public string DomainNetBIOSName { set; get; }
        public string ServicePrincipalName { set; get; }
        public bool EnablePasswordRotation { set; get; }
    }
}
