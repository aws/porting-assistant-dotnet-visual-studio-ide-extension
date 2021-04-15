using System;
using OmniSharp.Extensions.LanguageServer.Server;
using System.IO.Pipelines;
using System.Linq;
using System.Collections.Immutable;
using Task = System.Threading.Tasks.Task;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Microsoft.Extensions.DependencyInjection;
using PortingAssistant.Client.Client;
using PortingAssistant.Client.Model;
using PortingAssistantExtensionServer.Handlers;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using PortingAssistantExtensionServer.Common;
using PortingAssistantExtension.Telemetry;
using Serilog;
using PortingAssistantExtension.Telemetry.Interface;
using PortingAssistantExtensionServer.Models;

namespace PortingAssistantExtensionServer
{
    public class PortingAssistantLanguageServer
    {
        private readonly PipeReader _input;
        private readonly PipeWriter _output;
        private bool _started = false;
        private readonly Action<ILoggingBuilder> _logConfiguration;
        internal ILanguageServer _portingAssistantServer { get; private set; }
        private readonly PortingAssistantIDEConfiguration _configuration;

        public PortingAssistantLanguageServer(
            Action<ILoggingBuilder> logConfiguration,
            PipeReader input,
            PipeWriter output,
            PortingAssistantIDEConfiguration configuration
            )
        {
            _logConfiguration = logConfiguration;
            _input = input;
            _output = output;
            _configuration = configuration;
        }

        //TODO add handler interfaces for mock unit tests
        public async Task StartAsync()
        {
            _portingAssistantServer = await LanguageServer.From(options =>
            {
                options
                .WithInput(_input)
                .WithOutput(_output)
                .WithServices(service =>
                {
                    service.AddAssessment(_configuration.portingAssistantConfiguration);
                    service.AddSingleton<SolutionAnalysisService>();
                    service.AddSingleton<PortingService>();
                    service.AddSingleton<ITelemetryCollector>(sp =>
                    {
                        var logger = sp.GetService<ILogger<ITelemetryCollector>>();
                        return new TelemetryCollector(logger, _configuration.metricsFilePath);
                    });
                })
                .WithHandler<PortingAssistantTextSyncHandler>()
                .WithHandler<PortingAssistantCodeActionHandler>()
                .WithHandler<SolutionAssessmentHandler>()
                .WithHandler<PortingHandler>()
                .WithHandler<UpdateSettingsHandler>()
                .ConfigureLogging(_logConfiguration)
                .OnInitialized((instance, client, server, ct) =>
                {
                    if (server?.Capabilities?.CodeActionProvider?.Value?.CodeActionKinds != null)
                    {
                        server.Capabilities.CodeActionProvider.Value.CodeActionKinds = server.Capabilities.CodeActionProvider.Value.CodeActionKinds.ToImmutableArray().Remove(CodeActionKind.Empty).ToArray();
                    }
                    Console.WriteLine("Initialized ! We should use initialzed message to enable commands in client");
                    return Task.CompletedTask;
                });
            }).ConfigureAwait(false);
            _started = true;
        }

        public bool IsSeverStarted()
        {
            return _started;
        }

        public async Task WaitForShutdownAsync()
        {
            _started = false;
            await _portingAssistantServer.WaitForExit.ConfigureAwait(false);
        }


    }
}
