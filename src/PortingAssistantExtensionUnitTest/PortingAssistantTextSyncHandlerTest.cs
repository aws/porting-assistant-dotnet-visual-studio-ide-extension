using Codelyzer.Analysis.Model;
using CTA.Rules.Models;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using PortingAssistant.Client.Client;
using PortingAssistant.Client.Model;
using PortingAssistantExtensionServer;
using PortingAssistantExtensionServer.Common;
using PortingAssistantExtensionServer.Handlers;
using PortingAssistantExtensionServer.Models;
using PortingAssistantExtensionServer.TextDocumentModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PortingAssistantExtensionUnitTest
{
    public class PortingAssistantTextSyncHandlerTest
    {
        private Mock<ILanguageServerFacade> _languageServer;
        private Mock<ILogger<PortingAssistantTextSyncHandler>> _logger;
        private Mock<ILogger<SolutionAssessmentHandler>> _loggerSolutionHandler;
        private PortingAssistantTextSyncHandler _portingAssistantTextSyncHandler;
        private SolutionAssessmentHandler _solutionAssessmentHandler;
        private Mock<ITextDocumentLanguageServer> _textDocumentLanguageServer;

        private Mock<ILogger<AnalysisService>> _analysisLoggerMock;
        private Mock<ILogger<PortingService>> _portingLoggerMock;
        private Mock<IPortingAssistantClient> _clientMock;
        private AnalysisService _analysisService;
        private PortingService _portingService;

        private SolutionAnalysisResult _solutionAnalysisResult = TestParameters.TestSolutionAnalysisResult;
        private SourceFileAnalysisResult _sourceFileAnalysisResult = TestParameters.TestSourceFileAnalysisResult;

        private readonly AnalyzeSolutionRequest _analyzeSolutionRequest = new AnalyzeSolutionRequest
        {
            solutionFilePath = "pathToSolution",
            settings = new AnalyzerSettings { TargetFramework = "netcoreapp3.1" }
        };

        private static readonly string _testSolutionPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestSolution", "TestProject", "TestCodeFile.cs");
        private CodeFileDocument _codeFileDocument = new CodeFileDocument(DocumentUri.FromFileSystemPath(_testSolutionPath));

        private readonly DidChangeTextDocumentParams _didChangeTextDocumentParams = new DidChangeTextDocumentParams
        {
            TextDocument = new VersionedTextDocumentIdentifier
            {
                Version = 1,
                Uri = DocumentUri.FromFileSystemPath("\test\test")
            },
            ContentChanges = new Container<TextDocumentContentChangeEvent>
            (
                new List<TextDocumentContentChangeEvent>
                {
                    new TextDocumentContentChangeEvent
                    {
                        Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range
                        {
                            Start = new Position { Character = 0, Line = 10 },
                            End = new Position { Character = 10, Line = 10 }
                        },
                        RangeLength = 10,
                        Text = "using AspNet.Mvc"
                    }
                }
            )
        };

        private readonly DidOpenTextDocumentParams _didOpenTextDocumentParams = new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            {
                LanguageId = "testId",
                Text = "did open text document",
                Uri = DocumentUri.FromFileSystemPath("\\opentest\\test"),
                Version = 1
            }
        };

        private readonly DidCloseTextDocumentParams _didCloseTextDocumentParams = new DidCloseTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            {
                LanguageId = "testId",
                Text = "did close text document",
                Uri = DocumentUri.FromFileSystemPath("\\closetest\\test"),
                Version = 1
            }
        };

        private readonly DidSaveTextDocumentParams _didSaveTextDocumentParams = new DidSaveTextDocumentParams
        {
            Text = "did save text document new",
            TextDocument = new TextDocumentItem
            {
                LanguageId = "testId",
                Text = "did save text document",
                Uri = DocumentUri.FromFileSystemPath(_testSolutionPath),
                Version = 1
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
        public async Task HandleDidTextChangeSuccessAsync()
        {
            _analysisService._openDocuments =
               _analysisService._openDocuments.Add(DocumentUri.FromFileSystemPath("\test\test"), _codeFileDocument);

            await _portingAssistantTextSyncHandler.Handle(_didChangeTextDocumentParams, CancellationToken.None);
            Assert.AreEqual(_codeFileDocument.GetText(), "using AspNet.Mvc");
        }

        [Test]
        public async Task HandleDidOpenTextDocumentAsync()
        {
            await _solutionAssessmentHandler.Handle(_analyzeSolutionRequest, CancellationToken.None);

            await _portingAssistantTextSyncHandler.Handle(_didOpenTextDocumentParams, CancellationToken.None);

            Assert.IsTrue(_analysisService._openDocuments.ContainsKey(DocumentUri.FromFileSystemPath("\\opentest\\test")));
            Assert.AreEqual(_analysisService._openDocuments[DocumentUri.FromFileSystemPath("\\opentest\\test")].GetText(), "did open text document");
        }

        [Test]
        public async Task HandleDidCloseTextDocumentAsync()
        {
            _analysisService._openDocuments =
               _analysisService._openDocuments.Add(DocumentUri.FromFileSystemPath("\\closetest\\test"), _codeFileDocument);

            await _portingAssistantTextSyncHandler.Handle(_didCloseTextDocumentParams, CancellationToken.None);

            Assert.IsTrue(!_analysisService._openDocuments.ContainsKey(DocumentUri.FromFileSystemPath("\\closetest\\test")));
        }

        [Test]
        public async Task HandleDidSaveTextDocumentNoIncrementalAsync()
        {
            _analysisService._openDocuments =
               _analysisService._openDocuments.Add(DocumentUri.FromFileSystemPath(_testSolutionPath), _codeFileDocument);

            PALanguageServerConfiguration.EnabledContinuousAssessment = false;

            await _solutionAssessmentHandler.Handle(_analyzeSolutionRequest, CancellationToken.None);

            await _portingAssistantTextSyncHandler.Handle(_didSaveTextDocumentParams, CancellationToken.None);

            Assert.IsTrue(_analysisService._openDocuments.ContainsKey(DocumentUri.FromFileSystemPath(_testSolutionPath)));
        }


        [Test]
        public async Task HandleDidSaveTextDocumentAsync()
        {
            _analysisService._openDocuments =
               _analysisService._openDocuments.Add(DocumentUri.FromFileSystemPath(_testSolutionPath), _codeFileDocument);

            await _solutionAssessmentHandler.Handle(_analyzeSolutionRequest, CancellationToken.None);

            await _portingAssistantTextSyncHandler.Handle(_didSaveTextDocumentParams, CancellationToken.None);

            Assert.IsTrue(_analysisService._openDocuments.ContainsKey(DocumentUri.FromFileSystemPath(_testSolutionPath)));
            Assert.AreEqual(_analysisService._openDocuments[DocumentUri.FromFileSystemPath(_testSolutionPath)].GetText(), "did save text document new");
        }

        [Test]
        public void GetDocumentSuccess()
        {
            _analysisService._openDocuments =
               _analysisService._openDocuments.Add(DocumentUri.FromFileSystemPath(_testSolutionPath), _codeFileDocument);

            var result = _portingAssistantTextSyncHandler.GetDocument(_testSolutionPath);
            Assert.AreEqual(result.DocumentUri, DocumentUri.FromFileSystemPath(_testSolutionPath));
        }

        [Test]
        public void TryGetDocumentSuccess()
        {
            _analysisService._openDocuments =
               _analysisService._openDocuments.Add(DocumentUri.FromFileSystemPath(_testSolutionPath), _codeFileDocument);

            CodeFileDocument document;
            var result = _portingAssistantTextSyncHandler.TryGetDocument(_testSolutionPath, out document);
            Assert.AreEqual(document.DocumentUri, DocumentUri.FromFileSystemPath(_testSolutionPath));
        }

        [Test]
        public void GetTextDocumentAttributesSuccess()
        {
            _analysisService._openDocuments =
               _analysisService._openDocuments.Add(DocumentUri.FromFileSystemPath(_testSolutionPath), _codeFileDocument);

            var result = _portingAssistantTextSyncHandler.GetTextDocumentAttributes(DocumentUri.FromFileSystemPath(_testSolutionPath));

            Assert.AreEqual(result.LanguageId, "CSharpFileType");
        }

        [Test]
        public void ComparePathsSuccess()
        {
            var result = _portingAssistantTextSyncHandler.ComparePaths("\\testSolution\\testProject", "\\testSolution\\\\testProject");
            Assert.IsTrue(result);
        }

    }
}
