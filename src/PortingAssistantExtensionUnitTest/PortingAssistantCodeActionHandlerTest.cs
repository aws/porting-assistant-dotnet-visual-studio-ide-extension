using Codelyzer.Analysis.Model;
using CTA.Rules.Models;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using PortingAssistant.Client.Client;
using PortingAssistant.Client.Model;
using PortingAssistantExtensionServer;
using PortingAssistantExtensionServer.Common;
using PortingAssistantExtensionServer.Handlers;
using PortingAssistantExtensionServer.Models;
using PortingAssistantExtensionServer.TextDocumentModels;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace PortingAssistantExtensionUnitTest
{
    public class PortingAssistantCodeActionHandlerTest
    {
        private Mock<ILanguageServerFacade> _languageServer;
        private Mock<ILogger<PortingAssistantTextSyncHandler>> _logger;
        private Mock<ILogger<SolutionAssessmentHandler>> _loggerSolutionHandler;
        private PortingAssistantTextSyncHandler _portingAssistantTextSyncHandler;
        private SolutionAssessmentHandler _solutionAssessmentHandler;
        private PortingAssistantCodeActionHandler _portingAssistantCodeActionHandler;
        private Mock<ITextDocumentLanguageServer> _textDocumentLanguageServer;

        private Mock<ILogger<AnalysisService>> _analysisLoggerMock;
        private Mock<ILogger<PortingService>> _portingLoggerMock;
        private Mock<IPortingAssistantClient> _clientMock;
        private AnalysisService _analysisService;
        private PortingService _portingService;
        private static readonly string _testSolutionPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestSolution", "TestProject", "TestCodeFile.cs");
        private CodeFileDocument _codeFileDocument = new CodeFileDocument(DocumentUri.FromFileSystemPath(_testSolutionPath));

        private readonly AnalyzeSolutionRequest _analyzeSolutionRequest = new AnalyzeSolutionRequest
        {
            solutionFilePath = "pathToSolution",
            settings = new AnalyzerSettings { TargetFramework = "netcoreapp3.1" }
        };

        private SolutionAnalysisResult _solutionAnalysisResult = TestParameters.TestSolutionAnalysisResult;
        private SourceFileAnalysisResult _sourceFileAnalysisResult = TestParameters.TestSourceFileAnalysisResult;

        private static readonly Diagnostic _diagnostic = new Diagnostic
        {
            Range = new Range
            {
                Start = new Position { Character = 0, Line = 4 },
                End = new Position { Character = 21, Line = 4 }
            },
            Message = "Replace System.Web.Mvc namespace with Microsoft.AspNetCore.Mvc."
        };

        private CodeActionParams _codeActionParams = new CodeActionParams
        {
            Context = new CodeActionContext
            {
                Diagnostics = new Container<Diagnostic>
                (
                    new List<Diagnostic>
                    {
                        _diagnostic
                    }
                )
            },
            TextDocument = new TextDocumentIdentifier
            {
                Uri = _testSolutionPath
            },
            Range = new Range
            {
                Start = new Position { Character = 0, Line = 4 },
                End = new Position { Character = 21, Line = 4 }
            }
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
            _logger = new Mock<ILogger<PortingAssistantTextSyncHandler>>();

            _loggerSolutionHandler = new Mock<ILogger<SolutionAssessmentHandler>>();

            _solutionAssessmentHandler = new SolutionAssessmentHandler(_loggerSolutionHandler.Object, _languageServer.Object,
               _analysisService, _portingService);
            _portingAssistantTextSyncHandler = new PortingAssistantTextSyncHandler(_languageServer.Object, _analysisService,
                _logger.Object);

            _portingAssistantTextSyncHandler.SetCapability(new OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities.SynchronizationCapability
            {
                WillSave = true,
                DidSave = true,
                WillSaveWaitUntil = true
            });

            PALanguageServerConfiguration.EnabledContinuousAssessment = true;

            _portingAssistantCodeActionHandler = new PortingAssistantCodeActionHandler(_portingAssistantTextSyncHandler,
                _languageServer.Object, _analysisService);

            _portingAssistantCodeActionHandler.SetCapability(new OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities.CodeActionCapability
            {
                IsPreferredSupport = true,
                DataSupport = true,
                DisabledSupport = true
            });
        }

        [SetUp]
        public void Setup()
        {
            _clientMock.Setup(client => client.AnalyzeSolutionAsync(It.IsAny<string>(),
                It.IsAny<AnalyzerSettings>())).Returns(Task.FromResult(_solutionAnalysisResult));
            _clientMock.Setup(client => client.AnalyzeFileAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<List<string>>(),
                It.IsAny<RootNodes>(), It.IsAny<ExternalReferences>(), It.IsAny<AnalyzerSettings>()))
                .Returns(Task.FromResult(new List<SourceFileAnalysisResult> { _sourceFileAnalysisResult }));
            _languageServer.SetupGet(langaugeServer => langaugeServer.TextDocument)
                .Returns(_textDocumentLanguageServer.Object);
        }

        [Test]
        public async Task HandleCodeActionsSuccessAsync()
        {
            _analysisService._openDocuments =
               _analysisService._openDocuments.Add(DocumentUri.FromFileSystemPath(_testSolutionPath), _codeFileDocument);

            await _solutionAssessmentHandler.Handle(_analyzeSolutionRequest, CancellationToken.None);

            var actualCodeActions = await _portingAssistantCodeActionHandler.Handle(_codeActionParams, CancellationToken.None);

            Assert.AreEqual(actualCodeActions.Count(), 1);
            Assert.AreEqual(actualCodeActions.First().IsCodeAction, true);
            Assert.AreEqual(actualCodeActions.GetCodeActions().Count(), 1);
            Assert.AreEqual(actualCodeActions.GetCodeActions().First().Diagnostics.Count(), 1);
            Assert.AreEqual(actualCodeActions.GetCodeActions().First().Diagnostics.First(), _diagnostic);
            Assert.AreEqual(actualCodeActions.GetCodeActions().First().Kind, CodeActionKind.QuickFix);
            Assert.AreEqual(actualCodeActions.GetCodeActions().First().Title,
                "Replace System.Web.Mvc namespace with Microsoft.AspNetCore.Mvc.");
        }

        [Test]
        public void GetCodeActionRegistrationActions()
        {
            var result = _portingAssistantCodeActionHandler.GetRegistrationOptions();
            Assert.AreEqual(result.CodeActionKinds.Count(), 8);
        }


    }
}
