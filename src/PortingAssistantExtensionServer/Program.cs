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
                var portingAssistantConfiguration = JsonSerializer.Deserialize<PortingAssistantIDEConfiguration>(File.ReadAllText(config));
                var outputTemplate = Common.Constants.DefaultOutputTemplate;
                var isConsole = args.Length == 4 && args[3].Equals("--console");
                if (args.Length == 4 && !args[3].Equals("--console"))
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
                LogWatcher logWatcher = new LogWatcher(portingAssistantConfiguration.TelemetryConfiguration, Common.PALanguageServerConfiguration.AWSProfileName, "portingassistant-ide-");
                logWatcher.Start();

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
    }
}
