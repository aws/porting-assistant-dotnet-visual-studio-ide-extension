using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using OmniSharp.Extensions.LanguageServer.Protocol;
using PortingAssistant.Client.Client;
using PortingAssistant.Client.Model;
using PortingAssistantExtensionServer;
using PortingAssistantExtensionServer.Models;
using PortingAssistantExtensionTelemetry.Interface;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestParameters = PortingAssistantExtensionUnitTest.TestParameters;

namespace PortingAssistantExtensionUnitTest
{
    public class AnalysisServiceTest
    {
        private Mock<ILogger<AnalysisService>> _loggerMock;
        private Mock<IPortingAssistantClient> _clientMock;
        private Mock<ITelemetryCollector> _telemetryMock;
        private AnalysisService _analysisService;

        private SolutionAnalysisResult _solutionAnalysisResult = TestParameters.TestSolutionAnalysisResult;

        private AnalyzeSolutionRequest _analyzeSolutionRequest;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _clientMock = new Mock<IPortingAssistantClient>();
            _loggerMock = new Mock<ILogger<AnalysisService>>();
            _telemetryMock = new Mock<ITelemetryCollector>();

            _analysisService = new AnalysisService(_loggerMock.Object,
                _clientMock.Object, _telemetryMock.Object);
        }

        [SetUp]
        public void Setup()
        {
            _clientMock.Setup(client => client.AnalyzeSolutionAsync(It.IsAny<string>(),
                It.IsAny<AnalyzerSettings>())).Returns(Task.FromResult(_solutionAnalysisResult));
        }

        [Test]
        public async Task AssessSolutionSucceedsAsync()
        {
            _analyzeSolutionRequest = new AnalyzeSolutionRequest
            {
                solutionFilePath = "pathToSolution",
                settings = new AnalyzerSettings { TargetFramework = "netcoreapp3.1" }
            };

            var actualSolutionAnalysisResult = await _analysisService.AssessSolutionAsync(_analyzeSolutionRequest);

            Assert.AreEqual(_solutionAnalysisResult, actualSolutionAnalysisResult);
            _clientMock.Verify(_client => _client.AnalyzeSolutionAsync(It.IsAny<string>(),
                It.IsAny<AnalyzerSettings>()), Times.Exactly(1));
        }

        [Test]
        public async Task GetDiagnosticSucceedsAsync()
        {
            _analyzeSolutionRequest = new AnalyzeSolutionRequest
            {
                solutionFilePath = "pathToSolution",
                settings = new AnalyzerSettings { TargetFramework = "netcoreapp3.1" }
            };

            var actualSolutionAnalysisResult = await _analysisService.AssessSolutionAsync(_analyzeSolutionRequest);

            var diagnosticResult = await _analysisService.GetDiagnosticsAsync(Task.FromResult(actualSolutionAnalysisResult));
            Assert.AreEqual(diagnosticResult.Count, 1);
            Assert.AreEqual(diagnosticResult.Keys, new List<DocumentUri> { DocumentUri.FromFileSystemPath("/test/test") });
            Assert.AreEqual(diagnosticResult[DocumentUri.FromFileSystemPath("/test/test")].Count, 2);
            Assert.AreEqual(diagnosticResult[DocumentUri.FromFileSystemPath("/test/test")][0].Message, 
                "Porting Assistant: System.Web.Mvc.Controller.View() is incompatible for target framework netcoreapp3.1 Replace API with 12.0.3, Replace namespace with 12.0.3, Replace Source Package System.Web.Mvc-5.2.7 with 12.0.3, Upgrade Source Package System.Web.Mvc-5.2.7 to version 12.0.3");
            Assert.AreEqual(diagnosticResult[DocumentUri.FromFileSystemPath("/test/test")][1].Message, "Replace System.Web.Mvc namespace with Microsoft.AspNetCore.Mvc.");
        }

        [Test]
        public void TrimFilePathTest()
        {
            var result = _analysisService.TrimFilePath("\\testSolution\\testProject");
            Assert.AreEqual(result, "testSolutiontestProject");
        }

        [Test]
        public async Task DisposeTestAsync()
        {
            _analyzeSolutionRequest = new AnalyzeSolutionRequest
            {
                solutionFilePath = "pathToSolution",
                settings = new AnalyzerSettings { TargetFramework = "netcoreapp3.1" }
            };

            await _analysisService.AssessSolutionAsync(_analyzeSolutionRequest);
            _analysisService.Dispose();
            Assert.IsNull(_analysisService.FileToProjectAnalyssiResult);
            Assert.IsNull(_analysisService.CodeActions);
        }
    }
}

