using PortingAssistantExtensionClientShared.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistantVSExtensionClient.Models
{
    public class PortingAssistantIDEConfiguration
    {
        public TelemetryConfiguration TelemetryConfiguration { get; set; }

        public PortingAssistantIDEConfiguration()
        {
            TelemetryConfiguration = new TelemetryConfiguration();
        }
    }
    public class TelemetryConfiguration
    {
        private string AppData;
        public TelemetryConfiguration()
        {
            AppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string metricsFolder = Path.Combine(AppData, "Porting Assistant Extension", "logs");
            this.LogsPath = metricsFolder;
            this.LogFilePath = Path.Combine(metricsFolder, "portingAssistantExtension.log");
            this.MetricsFilePath = Path.Combine(metricsFolder, $"portingAssistantExtension-{DateTime.Today.ToString("yyyyMMdd")}.metrics");
            this.Suffix = new List<string>
            {
                ".log",
                ".metrics"
            };
        }
        public string InvokeUrl { get; set; }
        public string Region { get; set; }
        public string LogsPath { get; set; }
        public string ServiceName { get; set; }
        public string Description { get; set; }
        public List<string> Suffix { get; set; }
        public string LogFilePath { get; set; }
        public string MetricsFilePath { get; set; }
    }
}
