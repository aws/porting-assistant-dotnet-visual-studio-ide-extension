using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using PortingAssistant.Client.Client;
using PortingAssistant.Client.Model;
using PortingAssistantExtensionServer.Handlers;
using PortingAssistantExtensionServer.Models;
using PortingAssistantExtensionServer.Services;
using PortingAssistantExtensionUnitTest.Common;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TestParameters = PortingAssistantExtensionUnitTest.Common.TestParameters;

namespace PortingAssistantExtensionUnitTest
{
    public class SolutionAssessmentHandlerTest
    {
        private Mock<ILanguageServerFacade> _languageServer;
        private Mock<ILogger<SolutionAssessmentHandler>> _logger;
        private SolutionAssessmentHandler _solutionAssessmentHandler;
        private Mock<ITextDocumentLanguageServer> _textDocumentLanguageServer;

        private Mock<ILogger<AnalysisService>> _analysisLoggerMock;
        private Mock<ILogger<PortingService>> _portingLoggerMock;
        private Mock<IPortingAssistantClient> _clientMock;
        private AnalysisService _analysisService;
        private PortingService _portingService;

        private SolutionAnalysisResult _solutionAnalysisResult = TestParameters.TestSolutionAnalysisResult;

        private readonly AnalyzeSolutionRequest _analyzeSolutionRequest = new AnalyzeSolutionRequest
        {
            solutionFilePath = "pathToSolution",
            settings = new AnalyzerSettings { TargetFramework = "netcoreapp3.1" }
        };

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _clientMock = new Mock<IPortingAssistantClient>();
            _analysisLoggerMock = new Mock<ILogger<AnalysisService>>();
            _portingLoggerMock = new Mock<ILogger<PortingService>>();
            _textDocumentLanguageServer = new Mock<ITextDocumentLanguageServer>();

            _analysisService = new AnalysisService(_analysisLoggerMock.Object,
                _clientMock.Object);
            _portingService = new PortingService(_portingLoggerMock.Object,
                _clientMock.Object);


            _languageServer = new Mock<ILanguageServerFacade>();
            _logger = new Mock<ILogger<SolutionAssessmentHandler>>();

            _solutionAssessmentHandler = new SolutionAssessmentHandler(_logger.Object, _languageServer.Object,
                _analysisService, _portingService);
        }

        [SetUp]
        public void Setup()
        {
            _clientMock.Setup(client => client.AnalyzeSolutionAsync(It.IsAny<string>(),
                It.IsAny<AnalyzerSettings>())).Returns(Task.FromResult(_solutionAnalysisResult));
            _languageServer.SetupGet(langaugeServer => langaugeServer.TextDocument)
                .Returns(_textDocumentLanguageServer.Object);
        }

        [Test]
        public async Task SolutionAssessmentHandleSuccessAsync()
        {
            await _solutionAssessmentHandler.Handle(_analyzeSolutionRequest, CancellationToken.None);

            _clientMock.Verify(client => client.AnalyzeSolutionAsync(It.IsAny<string>(),
                It.IsAny<AnalyzerSettings>()), Times.Exactly(1));
        }

        [Test]
        public async Task SolutionAssessmentReturnsNullAsync()
        {
            var solutionAnalysisResult = new SolutionAnalysisResult
            {
                ProjectAnalysisResults = new List<ProjectAnalysisResult>
                {
                    new ProjectAnalysisResult
                    {
                        ProjectFilePath = "validfilepath",
                    },
                    new ProjectAnalysisResult
                    {
                        ProjectFilePath = null,
                    }
                }
            };

            await _portingService.GetPackageAnalysisResultAsync(Task.FromResult(solutionAnalysisResult));
            _portingLoggerMock.VerifyNoOtherCalls();
        }
    }
}
