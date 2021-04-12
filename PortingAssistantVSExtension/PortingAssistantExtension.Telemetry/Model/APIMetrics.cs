using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PortingAssistant.Client.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace PortingAssistantExtension.Telemetry.Model
{
    public class APIMetrics : MetricsBase
    {
        public string name { get; set; }
        public string nameSpace { get; set; }
        public string originalDefinition { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public Compatibility compatibility { get; set; }
        public string packageId { get; set; }
        public string packageVersion { get; set; }
    }
}
