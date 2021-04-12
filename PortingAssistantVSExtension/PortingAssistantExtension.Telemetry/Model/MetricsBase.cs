using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace PortingAssistantExtension.Telemetry.Model
{
    public class MetricsBase
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public MetricsType MetricsType { get; set; }
        public string PortingAssistantExtensionVersion { get; set; }
        public string TargetFramework { get; set; }
        public string TimeStamp { get; set; }
    }
}
