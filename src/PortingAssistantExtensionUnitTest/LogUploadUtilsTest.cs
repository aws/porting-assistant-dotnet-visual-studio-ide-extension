using Serilog;
using NUnit.Framework;
using PortingAssistantExtensionServer.Models;
using PortingAssistantExtensionTelemetry.Utils;

namespace PortingAssistantExtensionUnitTest
{
    internal class LogUploadUtilsTest
    {
        private PortingAssistantTelemetryConfiguration _config;

        [SetUp]
        public void Setup()
        {
            _config = new PortingAssistantTelemetryConfiguration
            {
                // set real endpoint for non mocked test
                InvokeUrl = "https://localhost",
                ServiceName = "encore",
                Region = "us-east-1"
            };
        }

        [Test]
        public void TestGetLogNameIde()
        {
            var logConfiguration = new LoggerConfiguration().Enrich.FromLogContext()
                .MinimumLevel.Warning();
            logConfiguration = logConfiguration.WriteTo.Console();
            Log.Logger = logConfiguration.CreateLogger();
            LogUploadUtils.InitializeUploader(true, _config, "default", false, Log.Logger);
            Assert.AreEqual("portingAssistant-ide-metrics",
                LogUploadUtils.Uploader.GetLogName("portingAssistantExtension20220802.metrics"));
            Assert.AreEqual("portingAssistant-ide-logs",
                LogUploadUtils.Uploader.GetLogName("portingAssistantExtension20220802.log"));
        }

        // test log upload util, depends on default profile
        // configured within the test environment
        public void TestLogUploadReal()
        {
            LogUploadUtils.InitializeUploader(true, _config, "default", false, Log.Logger);
            LogUploadUtils.OnTimedEvent(null, null);
        }
    }
}
