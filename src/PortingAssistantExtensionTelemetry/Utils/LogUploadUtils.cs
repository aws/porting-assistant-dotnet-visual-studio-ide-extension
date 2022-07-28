using PortingAssistantExtensionTelemetry.Model;
using System.IO;
using PortingAssistant.Client.Telemetry;
using Serilog;

namespace PortingAssistantExtensionTelemetry.Utils
{
    public static class LogUploadUtils
    {
        private static string GetLogName(string filePath)
        {
            var fileExtension = Path.GetExtension(filePath);
            if (fileExtension == ".metrics")
            {
                return "portingAssistant-ide-metrics";
            }
            if (fileExtension == ".log")
            {
                return "portingAssistant-ide-logs";
            }
            return "";
        }

        private static Uploader _uploader;

        public static void InitializeUploader(bool shareMetric,
            TelemetryConfiguration config,
            string profile,
            bool enabledDefaultCredentials,
            ILogger logger)
        {
            TelemetryClientFactory.TryGetClient(profile,
                config,
                out ITelemetryClient client,
                enabledDefaultCredentials);
            _uploader = new Uploader(config, client, logger, shareMetric)
            {
                GetLogName = GetLogName
            };
        }

        public static void OnTimedEvent(object source,
            System.Timers.ElapsedEventArgs e)
        {
            _uploader.Run();
        }

        public static void WriteLogUploadErrors()
        {
            _uploader.WriteLogUploadErrors();
        }
    }
}
