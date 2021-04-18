using System;
using System.IO.Pipes;
using System.IO.Pipelines;
using Task = System.Threading.Tasks.Task;
using Microsoft.Extensions.Logging;
using Serilog;
using Nerdbank.Streams;
using PortingAssistant.Client.Model;
using System.Threading.Tasks;
using System.Globalization;
using System.IO;
using OmniSharp.Extensions.LanguageServer.Server;
using PortingAssistantExtensionServer.Models;

namespace PortingAssistantExtensionServer
{
    public static class Program
    {

        private static bool _isConnected = false;
        static async Task Main(string[] args)
        {
            try
            {
                //TODO put settings in file/constant
                var stdInPipeName = @"extensionclientwritepipe";
                var stdOutPipeName = @"extensionclientreadpipe";
                var AppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var logRootPath = Path.Combine(AppData, "Porting Assistant Extension", "metrics");
                if (!Directory.Exists(logRootPath)) Directory.CreateDirectory(logRootPath);
                var logFilePath = Path.Combine(AppData, "Porting Assistant Extension", "logs", "portingAssistantExtension-{Date}.log");
                var metricsFilePath = Path.Combine(AppData, "Porting Assistant Extension", "metrics", $"portingAssistantExtension-{DateTime.Today.ToString("yyyyMMdd")}.log");
                var configuration = new PortingAssistantIDEConfiguration()
                {
                    portingAssistantConfiguration = new PortingAssistantConfiguration
                    {
                        DataStoreSettings = new DataStoreSettings()
                        {
                            HttpsEndpoint = "https://s3.us-west-2.amazonaws.com/aws.portingassistant.dotnet.datastore/",
                            S3Endpoint = "aws.portingassistant.dotnet.datastore",
                            GitHubEndpoint = "https://raw.githubusercontent.com/aws/porting-assistant-dotnet-datastore/master/"
                        }
                    },
                    metricsFilePath = metricsFilePath
                };

                if (args.Length != 0)
                {
                    stdInPipeName = args[0];
                    stdOutPipeName = args[1];
                }
                var (input, output) = await CreateNamedPipe(stdInPipeName, stdOutPipeName);
                var outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

                var isConsole = args.Length == 4 && args[3].Equals("--console");

                if (args.Length == 4 && !args[3].Equals("--console"))
                {
                    outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] (" + args[3] + ") {SourceContext}: {Message:lj}{NewLine}{Exception}";
                }

                Serilog.Formatting.Display.MessageTemplateTextFormatter tf = new Serilog.Formatting.Display.MessageTemplateTextFormatter(outputTemplate, CultureInfo.InvariantCulture);
                var logConfiguration = new LoggerConfiguration().Enrich.FromLogContext()
                    .MinimumLevel.Debug()
                    .WriteTo.RollingFile(
                        logFilePath,
                        outputTemplate: outputTemplate);

                if (isConsole)
                {
                    logConfiguration = logConfiguration.WriteTo.Console();
                }

                Log.Logger = logConfiguration.CreateLogger();

                var portingAssisstantLanguageServer = new PortingAssistantLanguageServer(
                    loggingBuilder => loggingBuilder
                    .SetMinimumLevel(LogLevel.Debug)
                    .AddLanguageProtocolLogging()
                    .AddSerilog(logger: Log.Logger, dispose: true),
                    input,
                    output,
                    configuration
                    );
                await portingAssisstantLanguageServer.StartAsync();
                await portingAssisstantLanguageServer.WaitForShutdownAsync();
                //TODO properly handle exit
                if (portingAssisstantLanguageServer.IsSeverStarted() && !_isConnected)
                {
                    await portingAssisstantLanguageServer.WaitForShutdownAsync();
                    Environment.Exit(0);
                }
            } catch (Exception e)
            {
                await Console.Error.WriteLineAsync(e.ToString());
                Environment.Exit(1);
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
