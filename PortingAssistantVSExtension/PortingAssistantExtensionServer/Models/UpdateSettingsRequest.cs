using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace PortingAssistantExtensionServer.Models
{
    class UpdateSettingsRequest : IRequest<bool>
    {
        public bool EnabledMetrics { get; set; }
        public bool EnabledContinuousAssessment { get; set; }
        public string CustomerEmail { get; set; }
        public string RootCacheFolder { get; set; }
    }
}
