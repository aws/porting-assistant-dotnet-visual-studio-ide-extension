using MediatR;
using PortingAssistantExtensionServer.Common;

namespace PortingAssistantExtensionServer.Models
{
    class UpdateSettingsRequest : IRequest<bool>
    {
        public bool EnabledMetrics { get; set; }
        public bool EnabledContinuousAssessment { get; set; }
        public string AWSProfileName { get; set; }

        public void UpdateSetting()
        {
            PALanguageServerConfiguration.EnabledMetrics = this.EnabledMetrics;
            PALanguageServerConfiguration.EnabledContinuousAssessment = this.EnabledContinuousAssessment;
            PALanguageServerConfiguration.AWSProfileName = this.AWSProfileName;
        }
        public override string ToString()
        {
            return $"EnabledMetrics: {EnabledMetrics},  " +
                $"EnabledContinuousAssessment: {EnabledContinuousAssessment}, " +
                $"AWSProfileName: {AWSProfileName}, ";
        }
    }
}
