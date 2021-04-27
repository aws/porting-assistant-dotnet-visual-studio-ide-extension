using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistantVSExtensionClient.Models
{
    class UpdateSettingsRequest
    {
        public bool EnabledMetrics { get; set; }
        public bool EnabledContinuousAssessment { get; set; }
        public string AWSProfileName { get; set; }
    }
}
