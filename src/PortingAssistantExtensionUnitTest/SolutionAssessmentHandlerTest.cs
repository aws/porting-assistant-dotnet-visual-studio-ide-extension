using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using PortingAssistant.Client.Client;
using PortingAssistant.Client.Model;
using PortingAssistantExtensionServer;
using PortingAssistantExtensionServer.Handlers;
using PortingAssistantExtensionServer.Models;
using PortingAssistantExtensionTelemetry.Interface;
using System.Threading;
using System.Threading.Tasks;

namespace PortingAssistantExtensionUnitTest
{
    public class SolutionAssessmentHandlerTest
    {
        private Mock<ILanguageServerFacade> _languageServer;
        private Mock<ILogger<SolutionAssessmentHandler>> _logger;
        private SolutionAssessmentHandler _solutionAssessmentHandler;
        private Mock<ITextDocumentLanguageServer> _textDocumentLanguageServer;

        private Mock<ILogger<AnalysisService>> _analysisLoggerMock;
        private Mock<IPortingAssistantClient> _clientMock;
        private Mock<ITelemetryCollector> _telemetryMock;
        private AnalysisService _analysisService;

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
            _telemetryMock = new Mock<ITelemetryCollector>();
            _textDocumentLanguageServer = new Mock<ITextDocumentLanguageServer>();

            _analysisService = new AnalysisService(_analysisLoggerMock.Object,
                _clientMock.Object, _telemetryMock.Object);

            _languageServer = new Mock<ILanguageServerFacade>();
            _logger = new Mock<ILogger<SolutionAssessmentHandler>>();

            _solutionAssessmentHandler = new SolutionAssessmentHandler(_logger.Object, _languageServer.Object,
                _analysisService);
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
    }
}
