using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistantVSExtensionClient.Models
{
    public class DeploymentParameters
    {
        public String BuildFolderPath { set; get; }
        public String DirectoryId { set; get; }
        public String DomainSecretsArn { set; get; }

        public String DomainDNSName { set; get; }
        public String DomainNetBIOSName { set; get; }
        public String ServicePrincipalName { set; get; }
        public Boolean EnablePasswordRotation { set; get; }
    }
}
