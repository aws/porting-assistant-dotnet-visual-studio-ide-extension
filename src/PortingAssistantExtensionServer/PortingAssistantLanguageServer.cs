using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Server;
using PortingAssistant.Client.Client;
using PortingAssistantExtensionServer.Handlers;
using PortingAssistantExtensionServer.Models;
using System;
using System.Collections.Immutable;
using System.IO.Pipelines;
using System.Linq;
using Task = System.Threading.Tasks.Task;

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
                    service.AddAssessment(_configuration.PortingAssistantConfiguration);
                    service.AddSingleton<AnalysisService>();
                    service.AddSingleton<PortingService>();
                })
                .WithHandler<PortingAssistantTextSyncHandler>()
                .WithHandler<PortingAssistantCodeActionHandler>()
                .WithHandler<SolutionAssessmentHandler>()
                .WithHandler<PortingHandler>()
                .WithHandler<UpdateSettingsHandler>()
                .WithMaximumRequestTimeout(TimeSpan.FromHours(2))
                .ConfigureLogging(_logConfiguration)
                .OnInitialize((server, request, ct) =>
                {
                    if (request?.InitializationOptions is JObject initOption)
                    {
                        var settings = initOption?["paSettings"].ToObject<UpdateSettingsRequest>();
                        settings.UpdateSetting();
                    }
                    return Task.CompletedTask;
                })
                .OnInitialized((instance, client, server, ct) =>
                {
                    if (server?.Capabilities?.CodeActionProvider?.Value?.CodeActionKinds != null)
                    {
                        server.Capabilities.CodeActionProvider.Value.CodeActionKinds = server.Capabilities.CodeActionProvider.Value.CodeActionKinds.ToImmutableArray().Remove(CodeActionKind.Empty).ToArray();
                    }
                    Console.WriteLine("Initialized !");
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
