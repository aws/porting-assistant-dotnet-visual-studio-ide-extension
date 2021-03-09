using System;
using OmniSharp.Extensions.LanguageServer;
using OmniSharp.Extensions.LanguageServer.Server;
using System.IO.Pipes;
using System.IO.Pipelines;
using System.Linq;
using System.Collections.Immutable;
using System.Windows;
using Task = System.Threading.Tasks.Task;
using System.IO;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Security.AccessControl;
using Microsoft.Extensions.DependencyInjection;
using Nerdbank.Streams;

using PortingAssistant.Client.Client;
using PortingAssistant.Client.Model;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using PortingAssistantExtensionServer.Handlers;

namespace PortingAssistantExtensionServer
{
    public static class Program
    {
        static async Task Main(string[] args)
        {

                var (input, output) = await CreateNamedPipe();
                var configuration = new PortingAssistantConfiguration();
                configuration.DataStoreSettings.HttpsEndpoint = "https://s3.us-west-2.amazonaws.com/aws.portingassistant.dotnet.datastore/";
                configuration.DataStoreSettings.S3Endpoint = "aws.portingassistant.dotnet.datastore";
                configuration.DataStoreSettings.GitHubEndpoint = "https://raw.githubusercontent.com/aws/porting-assistant-dotnet-datastore/master/";
                var server = await LanguageServer.From(options => ConfigureServer(options, input, output, configuration));
                await server.WaitForExit;
            
            
        }


        public static void ConfigureServer(LanguageServerOptions options, PipeReader input, PipeWriter output, PortingAssistantConfiguration config)
        {
            options
                .WithInput(input)
                .WithOutput(output)
                .WithServices(service => {
                    service.AddAssessment(config);
                    service.AddSingleton<SolutionAnalysisService>();
                    service.AddSingleton<PortingService>();
                    service.AddSingleton(new ConfigurationItem() { Section = "csharp" });
                })
                .WithHandler<PortingAssistantTextSyncHandler>()
                .WithHandler<SolutionAssessmentHandler>()
                .WithHandler<PortingHandler>()
                .ConfigureLogging(
                    x => x
                        .ClearProviders()
                        .AddLanguageProtocolLogging()
                        .SetMinimumLevel(LogLevel.Debug)
                )
                .OnInitialized((instance, client, server, ct) =>
                {
                  
                    if (server?.Capabilities?.CodeActionProvider?.Value?.CodeActionKinds != null)
                    {
                        server.Capabilities.CodeActionProvider.Value.CodeActionKinds = server.Capabilities.CodeActionProvider.Value.CodeActionKinds.ToImmutableArray().Remove(CodeActionKind.Empty).ToArray();
                    }
                    Console.WriteLine("Initialized ! We should use initialzed message to enable commands in client");
                    return Task.CompletedTask;
                });
        }

        private static async Task<(PipeReader input, PipeWriter output)> CreateNamedPipe()
        {

            Console.InputEncoding = System.Text.Encoding.UTF8;
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var stdInPipeName = @"clientwritepipe";
            var stdOutPipeName = @"clientreadpipe";
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

        public static IServiceCollection ConfigureSection<TOptions>(this IServiceCollection services, string? sectionName)
            where TOptions : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            
            services.AddOptions()
                .AddSingleton<IOptionsChangeTokenSource<TOptions>>(
                _ => new ConfigurationChangeTokenSource<TOptions>(
                    Options.DefaultName,
                    sectionName == null ? _.GetRequiredService<IConfiguration>() : _.GetRequiredService<IConfiguration>().GetSection(sectionName)
                )
            );
            return services.AddSingleton<IConfigureOptions<TOptions>>(
                _ => new NamedConfigureFromConfigurationOptions<TOptions>(
                    Options.DefaultName,
                    sectionName == null ? _.GetRequiredService<IConfiguration>() : _.GetRequiredService<IConfiguration>().GetSection(sectionName)
                )
            );
        }
    }
}
