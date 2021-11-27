using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PortingAssistant.Client.Client;
using PortingAssistant.Client.Model;
using PortingAssistantExtensionServer;
using PortingAssistantExtensionServer.Handlers;
using PortingAssistantExtensionServer.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using System.Linq;
using System.Collections.Generic;

namespace PortingAssistantExtensionUnitTest
{
    class TestDeploymentHandlerTest
    {
        private Mock<ILogger<TestDeploymentHandler>> _logger;
        private Mock<TestDeploymentService> _testDeploymentService;
        private Mock<ILanguageServerFacade> _languageServer;
        private Mock<IPortingAssistantClient> _clientMock;
        private Mock<ILogger<TestDeploymentService>> _serviceLogger;
        private Mock<TestDeploymentHandler> _handler;
        private Mock<IRemoteCallUtils> _remoteCallUtils;
        private TestDeploymentHandler _testDeploymentHandler;
        private readonly TestDeploymentRequest _testDeploymentRequest = new TestDeploymentRequest
        {
            excutionType = new string("RunCommand"),
            command = new string("App2Container-like.exe"),
            arguments = new List<string> { "arg1", "arg2" }
        };

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _logger = new Mock<ILogger<TestDeploymentHandler>>();
            _serviceLogger = new Mock<ILogger<TestDeploymentService>>();
            _clientMock = new Mock<IPortingAssistantClient>();
            _languageServer = new Mock<ILanguageServerFacade>();

            _remoteCallUtils = new Mock<IRemoteCallUtils>();
            _testDeploymentService = new Mock<TestDeploymentService>(_serviceLogger.Object,
                                                                        _remoteCallUtils.Object);

            _remoteCallUtils
                .Setup(x => x.Execute(It.IsAny<string>(),
                        It.IsAny<List<string>>(),
                        It.IsAny<int>()))
                .Returns(0);

            _testDeploymentHandler = new TestDeploymentHandler(_logger.Object, _languageServer.Object,
                                                        _testDeploymentService.Object);
        }

        [Test]
        public async Task TestDeploymentHandlerExecute()
        {
            var actualResult = await _testDeploymentHandler.Handle(_testDeploymentRequest, CancellationToken.None);

            Assert.AreEqual(actualResult.status, 0);
        }

    }
}
