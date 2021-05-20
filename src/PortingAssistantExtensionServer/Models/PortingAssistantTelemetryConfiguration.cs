using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PortingAssistantExtensionTelemetry.Model;

namespace PortingAssistantExtensionServer.Models
{
    public class PortingAssistantTelemetryConfiguration : TelemetryConfiguration
    {
        private string AppData;
        public PortingAssistantTelemetryConfiguration()
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
    }
}
