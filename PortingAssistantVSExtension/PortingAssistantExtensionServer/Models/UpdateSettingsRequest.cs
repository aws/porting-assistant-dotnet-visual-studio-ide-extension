using MediatR;
using PortingAssistantExtensionServer.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace PortingAssistantExtensionServer.Models
{
    class UpdateSettingsRequest : IRequest<bool>
    {
        public bool EnabledMetrics { get; set; }
        public bool EnabledContinuousAssessment { get; set; }
        public string AWSProfileName { get; set; }
        public string RootCacheFolder { get; set; }

        public void UpdateSetting()
        {
            PALanguageServerConfiguration.EnabledMetrics = this.EnabledMetrics;
            PALanguageServerConfiguration.EnabledContinuousAssessment = this.EnabledContinuousAssessment;
            PALanguageServerConfiguration.AWSProfileName = this.AWSProfileName;
            PALanguageServerConfiguration.RootCacheFolder = this.RootCacheFolder;
        }
    }
}
