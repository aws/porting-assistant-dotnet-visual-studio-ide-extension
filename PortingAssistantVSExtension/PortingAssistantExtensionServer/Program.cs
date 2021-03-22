using System;
using OmniSharp.Extensions.LanguageServer.Server;
using System.IO.Pipes;
using System.IO.Pipelines;
using System.Linq;
using System.Collections.Immutable;
using System.Windows;
using Task = System.Threading.Tasks.Task;
using System.IO;
using Microsoft.Extensions.Logging;
using Serilog;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Security.AccessControl;
using Microsoft.Extensions.DependencyInjection;
using Nerdbank.Streams;

using PortingAssistant.Client.Client;
using PortingAssistant.Client.Model;
using System.Threading.Tasks;
using PortingAssistantExtensionServer.Handlers;
using System.Globalization;

namespace PortingAssistantExtensionServer
{
    public static class Program
    {
        static async Task Main(string[] args)
        {
            var stdInPipeName = @"clientwritepipe";
            var stdOutPipeName = @"clientreadpipe";
            var (input, output) = await CreateNamedPipe(stdInPipeName, stdOutPipeName);
            var configuration = new PortingAssistantConfiguration()
            {
                DataStoreSettings = new DataStoreSettings()
                {
                    HttpsEndpoint = "https://s3.us-west-2.amazonaws.com/aws.portingassistant.dotnet.datastore/",
                    S3Endpoint = "aws.portingassistant.dotnet.datastore",
                    GitHubEndpoint = "https://raw.githubusercontent.com/aws/porting-assistant-dotnet-datastore/master/"
                }
              };
            var outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

            if (args.Length == 4 && !args[3].Equals("--console"))
            {
                outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] (" + args[3] + ") {SourceContext}: {Message:lj}{NewLine}{Exception}";
            }
            Serilog.Formatting.Display.MessageTemplateTextFormatter tf = new Serilog.Formatting.Display.MessageTemplateTextFormatter(outputTemplate, CultureInfo.InvariantCulture);
            var portingAssisstantLanguageServer = new PortingAssistantLanguageServer(
                loggingBuilder => loggingBuilder
                .SetMinimumLevel(LogLevel.Debug)
                .AddSerilog(logger: Log.Logger, dispose: true),
                input,
                output,
                configuration
                );

            await portingAssisstantLanguageServer.StartAsync();
            await portingAssisstantLanguageServer.WaitForShutdownAsync();            
        }

        private static async Task<(PipeReader input, PipeWriter output)> CreateNamedPipe(string stdInPipeName, string stdOutPipeName)
        {
            //shutdown server when lost connection
            Console.InputEncoding = System.Text.Encoding.UTF8;
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var readerPipe = new NamedPipeServerStream(stdInPipeName, PipeDirection.In, maxNumberOfServerInstances: 1, transmissionMode: PipeTransmissionMode.Byte, options: System.IO.Pipes.PipeOptions.Asynchronous);
            var writerPipe = new NamedPipeServerStream(stdOutPipeName, PipeDirection.Out, maxNumberOfServerInstances: 1, transmissionMode: PipeTransmissionMode.Byte, options: System.IO.Pipes.PipeOptions.Asynchronous);
            Console.WriteLine("Waiting for client to connect on pipe...");
            await readerPipe.WaitForConnectionAsync();
            await writerPipe.WaitForConnectionAsync();
            var pipeline1 = readerPipe.UsePipe();
            var pipeline2 = writerPipe.UsePipe();
            // await pipe.WaitForConnectionAsync().ConfigureAwait(false);
            if (readerPipe.IsConnected && writerPipe.IsConnected)
            {
                 Console.WriteLine("Connected");
            }
            return (pipeline1.Input, pipeline2.Output);
        }
    }
}
