using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PortingAssistant.Client.Model;

namespace PortingAssistantExtensionTelemetry.Model
{
    public class CodeFileMetrics : MetricsBase
    {
        [JsonProperty("filePath")]
        public string FilePath { get; set; }

        [JsonProperty("diagnostics")]
        public int Diagnostics { get; set; }
    }
}
