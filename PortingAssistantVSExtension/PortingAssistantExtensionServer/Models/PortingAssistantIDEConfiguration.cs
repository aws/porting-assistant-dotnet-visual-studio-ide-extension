using PortingAssistant.Client.Model;
using PortingAssistantExtensionTelemetry.Model;
using System.Collections.Generic;

namespace PortingAssistantExtensionServer.Models
{
    public class PortingAssistantIDEConfiguration
    {
        public PortingAssistantConfiguration PortingAssistantConfiguration { get; set; }
        
        public TelemetryConfiguration TelemetryConfiguration { get; set; }

    }
}
