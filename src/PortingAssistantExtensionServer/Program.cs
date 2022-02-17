using System;
using System.IO.Pipes;
using System.IO.Pipelines;
using Task = System.Threading.Tasks.Task;
using Microsoft.Extensions.Logging;
using Serilog;
using Nerdbank.Streams;
using System.Threading.Tasks;
using System.Globalization;
using System.IO;
using OmniSharp.Extensions.LanguageServer.Server;
using PortingAssistantExtensionServer.Models;
using PortingAssistantExtensionTelemetry;
using System.Text.Json;
using System.Net.Http;
using PortingAssistantExtensionTelemetry.Utils;

namespace PortingAssistantExtensionServer
{
    public static class Program
    {

        private static bool _isConnected = false;
        static async Task Main(string[] args)
        {
            try
            {
                if (args.Length < 1)
                {
                    throw new ArgumentException("Must provide a config file");
                }
                var config = args[0];
                var stdInPipeName = args.Length == 1 ? Common.Constants.stdDebugInPipeName : args[1];
                var stdOutPipeName = args.Length == 1 ? Common.Constants.stdDebugOutPipeName : args[2];
                Common.PALanguageServerConfiguration.ExtensionVersion = args.Length == 1 ? "0.0.0" : args[3];
                var vsClientVersion = args.Length == 1 ? Common.Constants.VS_UNKNOWN : args[4];
                Common.PALanguageServerConfiguration.VisualStudioVersion = GetVSVersion(vsClientVersion);
                Console.WriteLine($"Porting Assistant Version is {Common.PALanguageServerConfiguration.ExtensionVersion}");
                Console.WriteLine($"Visual Studio Version is {Common.PALanguageServerConfiguration.VisualStudioVersion}");
                var portingAssistantConfiguration = JsonSerializer.Deserialize<PortingAssistantIDEConfiguration>(File.ReadAllText(config));
                
                var outputTemplate = Common.Constants.DefaultOutputTemplate;
                var isConsole = args.Length == 5 && args[4].Equals("--console");
                if (args.Length == 5 && !args[4].Equals("--console"))
                {
                    outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] (Porting Assistant IDE Extension) (" + args[3] + ") {SourceContext}: {Message:lj}{NewLine}{Exception}";
                }
                Serilog.Formatting.Display.MessageTemplateTextFormatter tf = new Serilog.Formatting.Display.MessageTemplateTextFormatter(outputTemplate, CultureInfo.InvariantCulture);
                var logConfiguration = new LoggerConfiguration().Enrich.FromLogContext()
                    .MinimumLevel.Warning()
                    .WriteTo.File(
                        portingAssistantConfiguration.TelemetryConfiguration.LogFilePath,
                        rollingInterval: RollingInterval.Day,
                        rollOnFileSizeLimit: true,
                        outputTemplate: outputTemplate);
                if (isConsole)
                {
                    logConfiguration = logConfiguration.WriteTo.Console();
                }
                Log.Logger = logConfiguration.CreateLogger();
                TelemetryCollector.Builder(Log.Logger, portingAssistantConfiguration.TelemetryConfiguration.MetricsFilePath);
                var (input, output) = await CreateNamedPipe(stdInPipeName, stdOutPipeName);
                var portingAssisstantLanguageServer = new PortingAssistantLanguageServer(
                    loggingBuilder => loggingBuilder
                    .SetMinimumLevel(LogLevel.Debug)
                    .AddLanguageProtocolLogging()
                    .AddSerilog(logger: Log.Logger, dispose: true),
                    input,
                    output,
                    portingAssistantConfiguration
                    );
                await portingAssisstantLanguageServer.StartAsync();

                var logTimer = new System.Timers.Timer();
                
                logTimer.Interval = Convert.ToDouble(portingAssistantConfiguration.TelemetryConfiguration.LogTimerInterval);
                var lastReadTokenFile = Path.Combine(portingAssistantConfiguration.TelemetryConfiguration.LogsPath, "lastToken.json");
                logTimer.Elapsed += (source, e) => LogUploadUtils.OnTimedEvent(source, e, portingAssistantConfiguration.TelemetryConfiguration, lastReadTokenFile, Common.PALanguageServerConfiguration.AWSProfileName); ;
                logTimer.AutoReset = true;
                logTimer.Enabled = true;
                
                await portingAssisstantLanguageServer.WaitForShutdownAsync();

                if (portingAssisstantLanguageServer.IsSeverStarted() && !_isConnected)
                {
                    await portingAssisstantLanguageServer.WaitForShutdownAsync();
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Porting Assistant Extension failed with error: ");
                Environment.Exit(1);
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static async Task<(PipeReader input, PipeWriter output)> CreateNamedPipe(string stdInPipeName, string stdOutPipeName)
        {
            Console.InputEncoding = System.Text.Encoding.UTF8;
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var readerPipe = new NamedPipeClientStream(serverName: ".", pipeName: stdInPipeName, direction: PipeDirection.In, options: System.IO.Pipes.PipeOptions.Asynchronous);
            var writerPipe = new NamedPipeClientStream(serverName: ".", pipeName: stdOutPipeName, direction: PipeDirection.Out, options: System.IO.Pipes.PipeOptions.Asynchronous);
            Console.WriteLine("Waiting for connection on pipe...");
            await readerPipe.ConnectAsync();
            await writerPipe.ConnectAsync();
            var pipeline1 = readerPipe.UsePipe();
            var pipeline2 = writerPipe.UsePipe();
            if (readerPipe.IsConnected && writerPipe.IsConnected)
            {
                _isConnected = true;
                Console.WriteLine("Connected");
            }
            return (pipeline1.Input, pipeline2.Output);
        }

        private static string GetVSVersion(string vsClientVersion)
        {
            
            try
            {
                var vs2022 = Version.Parse("17.0");
                var version = Version.Parse(vsClientVersion);
                if (version.CompareTo(vs2022) >= 0) return Common.Constants.VS2022;
                else return Common.Constants.VS2019;
            }
            catch (Exception)
            {
                return Common.Constants.VS_UNKNOWN;
            }
        }
    }
}
