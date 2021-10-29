using OmniSharp.Extensions.JsonRpc;
using PortingAssistantExtensionServer.Models;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using System.Linq;
using System.Collections.Generic;

namespace PortingAssistantExtensionServer.Handlers
{
    [Serial, Method("deploySolution")]
    internal interface ITestDeploymentHandler : IJsonRpcRequestHandler<TestDeploymentRequest, TestDeploymentResponse> { }
    internal class TestDeploymentHandler : ITestDeploymentHandler
    {

        private readonly ILogger<ITestDeploymentHandler> _logger;
        private readonly TestDeploymentService _testDeploymentService;
        private readonly ILanguageServerFacade _languageServer;
        public TestDeploymentHandler(ILogger<TestDeploymentHandler> logger,
            ILanguageServerFacade languageServer,
            TestDeploymentService testDeploymentService
            )
        {
            _logger = logger;
            _languageServer = languageServer;
            _testDeploymentService = testDeploymentService;
        }

        public async Task<TestDeploymentResponse> Handle(TestDeploymentRequest request, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"received deployment request: ${request.fileName + string.Join(" ", request.arguments)} .....");
            var result = _testDeploymentService.Excute(request);
            return new TestDeploymentResponse()
            {
                status = result,
            };
        }
    }
}
