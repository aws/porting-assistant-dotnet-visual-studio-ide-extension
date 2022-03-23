using MediatR;
using PortingAssistantExtensionServer.Common;

namespace PortingAssistantExtensionServer.Models
{
    class UpdateSettingsRequest : IRequest<bool>
    {
        public bool EnabledMetrics { get; set; }
        public bool EnabledContinuousAssessment { get; set; }
        public string AWSProfileName { get; set; }
        public bool EnabledDefaultCredentials { get; set; } = false;

        public void UpdateSetting()
        {
            PALanguageServerConfiguration.EnabledMetrics = this.EnabledMetrics;
            PALanguageServerConfiguration.EnabledContinuousAssessment = this.EnabledContinuousAssessment;
            PALanguageServerConfiguration.AWSProfileName = this.AWSProfileName;
            PALanguageServerConfiguration.EnabledDefaultCredentials = this.EnabledDefaultCredentials;
        }
        public override string ToString()
        {
            return $"EnabledMetrics: {EnabledMetrics},  " +
                $"EnabledContinuousAssessment: {EnabledContinuousAssessment}, " +
                $"AWSProfileName: {AWSProfileName}, " +
                $"EnabledDefaultCredentials: {EnabledDefaultCredentials}";
        }
    }
}
