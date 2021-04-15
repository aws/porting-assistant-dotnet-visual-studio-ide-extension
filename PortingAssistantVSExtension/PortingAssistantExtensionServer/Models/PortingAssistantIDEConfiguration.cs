using PortingAssistant.Client.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace PortingAssistantExtensionServer.Models
{
    public class PortingAssistantIDEConfiguration
    {
        public PortingAssistantConfiguration portingAssistantConfiguration { get; set; }
        public string metricsFilePath { get; set; }

    }
}
