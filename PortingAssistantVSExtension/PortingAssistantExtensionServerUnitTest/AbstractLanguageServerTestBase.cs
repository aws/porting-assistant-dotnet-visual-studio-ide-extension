using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.JsonRpc.Testing;
using OmniSharp.Extensions.LanguageProtocol.Testing;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit.Abstractions;
using PortingAssistantExtensionServer;
using PortingAssistant.Client.Model;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using System.Collections.Concurrent;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using PortingAssistantExtensionServer.Models;

namespace PortingAssistantExtensionServerUnitTest
{
    public class AbstractLanguageServerTestBase : LanguageServerTestBase
    {
        private readonly ITestOutputHelper _output;
        private PortingAssistantIDEConfiguration portingAssistantIDEConfiguration;
        private PortingAssistantLanguageServer _portingAssistantLanguageServer;
        private ILanguageClient _client;
        private readonly ConcurrentDictionary<DocumentUri, IEnumerable<Diagnostic>> _diagnostics =
            new ConcurrentDictionary<DocumentUri, IEnumerable<Diagnostic>>();

        protected ILogger Logger { get; }

        public AbstractLanguageServerTestBase(ITestOutputHelper output,
            PortingAssistantIDEConfiguration configuration = null) : base(
            new JsonRpcTestOptions()
        )
        {
            _output = output;
            portingAssistantIDEConfiguration = configuration;
            SetupServer();
            SetupClient();
        }

        protected override (Stream clientOutput, Stream serverInput) SetupServer()
        {
            var clientPipe = new Pipe(TestOptions.DefaultPipeOptions);
            var serverPipe = new Pipe(TestOptions.DefaultPipeOptions);
            _portingAssistantLanguageServer = new PortingAssistantLanguageServer(
                logConfig => logConfig.AddConsole(),
                clientPipe.Reader,
                serverPipe.Writer,
                portingAssistantIDEConfiguration
                );
            return (serverPipe.Reader.AsStream(), clientPipe.Writer.AsStream());
        }

        private void SetupClient()
        {
            var task = InitializeClientWithConfiguration(x =>
            {
                x.OnPublishDiagnostics(result =>
                {
                    _diagnostics.AddOrUpdate(result.Uri, result.Diagnostics,
                        (a, b) => result.Diagnostics);
                });
            });
            task.Wait();
            _client = task.Result.client;
        }


    }
}
