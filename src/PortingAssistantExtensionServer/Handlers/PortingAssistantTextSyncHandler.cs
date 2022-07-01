using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol;
using MediatR;
using System.Threading;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using System.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using System.IO;
using PortingAssistantExtensionServer.TextDocumentModels;
using Microsoft.Extensions.Logging;
using System;
using PortingAssistantExtensionServer.Services;

namespace PortingAssistantExtensionServer.Handlers
{
    class PortingAssistantTextSyncHandler : ITextDocumentSyncHandler
    {
        private readonly TextDocumentChangeRegistrationOptions _options;
        private readonly TextDocumentSaveRegistrationOptions _saveOptions;
        private readonly ILanguageServerFacade languageServer;
        private readonly AnalysisService _solutionAnalysisService;
        private readonly ILogger<PortingAssistantTextSyncHandler> _logger;
        private SynchronizationCapability _capability;

        public void SetCapability(SynchronizationCapability capability) { _capability = capability; }
        public TextDocumentChangeRegistrationOptions GetRegistrationOptions() { return _options; }
        TextDocumentRegistrationOptions IRegistration<TextDocumentRegistrationOptions>.GetRegistrationOptions() { return _options; }
        TextDocumentSaveRegistrationOptions IRegistration<TextDocumentSaveRegistrationOptions>.GetRegistrationOptions() { return _saveOptions; }

        public PortingAssistantTextSyncHandler(ILanguageServerFacade languageServer, AnalysisService solutionAnalysisService, ILogger<PortingAssistantTextSyncHandler> logger)
        {
            _options = new TextDocumentChangeRegistrationOptions()
            {
                DocumentSelector = new DocumentSelector(DocumentSelector.ForPattern("**/*.cs").Concat(DocumentSelector.ForScheme("CSharpFileType"))),
                SyncKind = TextDocumentSyncKind.Full
            };
            _saveOptions = new TextDocumentSaveRegistrationOptions()
            {
                DocumentSelector = _options.DocumentSelector,
                IncludeText = true
            };
            this.languageServer = languageServer;
            _solutionAnalysisService = solutionAnalysisService;
            _logger = logger;
        }

        public CodeFileDocument? GetDocument(DocumentUri documentUri)
        {
            return _solutionAnalysisService._openDocuments.TryGetValue(documentUri, out var value) ? value : null;
        }
        public bool TryGetDocument(DocumentUri documentUri, out CodeFileDocument document)
        {
            return _solutionAnalysisService._openDocuments.TryGetValue(documentUri, out document);
        }

        public TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
        {
            if (uri.Path.EndsWith(".cs"))
            {
                return new TextDocumentAttributes(uri, "CSharpFileType");
            }

            if (uri.Path.EndsWith(".vb"))
            {
                return new TextDocumentAttributes(uri, "VisualBasicFileType");
            }
            return null;
        }

        public Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
        {
            if (!_solutionAnalysisService._openDocuments.TryGetValue(request.TextDocument.Uri, out var document)) return Unit.Task;
            document.Load(request.ContentChanges.FirstOrDefault().Text);
            return Unit.Task;

        }

        public bool ComparePaths(string p1, string p2)
        {
            return
                Path.Combine(p1.Split(new string[] { @"\", @"/" }, System.StringSplitOptions.RemoveEmptyEntries))
                .Equals(
                Path.Combine(p2.Split(new string[] { @"\", @"/" }, System.StringSplitOptions.RemoveEmptyEntries))
                    );
        }

        public Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
        {
            var document = new CodeFileDocument(request.TextDocument.Uri);

            lock (_solutionAnalysisService._openDocuments)
            {
                _solutionAnalysisService._openDocuments = _solutionAnalysisService._openDocuments.Add(request.TextDocument.Uri, document);
            }

            document.Load(request.TextDocument.Text);
            if (_solutionAnalysisService.HasSolutionAnalysisResult())
            {
                Process(document);
            }

            return Unit.Task;

        }
        public Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
        {
            lock (_solutionAnalysisService._openDocuments)
            {
                _solutionAnalysisService._openDocuments = _solutionAnalysisService._openDocuments.Remove(request.TextDocument.Uri);
            }
            return Unit.Task;
        }
        public Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
        {
            if (!_capability.DidSave || !Common.PALanguageServerConfiguration.EnabledContinuousAssessment) return Unit.Task;
            if (_solutionAnalysisService._openDocuments.TryGetValue(request.TextDocument.Uri, out var document))
            {
                document.Load(request.Text);
                if (_solutionAnalysisService.HasSolutionAnalysisResult())
                {
                    Process(document);
                }
            }
            return Unit.Task;
        }

        private async void Process(CodeFileDocument document)
        {
            if (document == null || document.DocumentUri == null) {
                return;
            }
            try
            {
                var diagnostics = _solutionAnalysisService.GetDiagnosticsAsync(document.DocumentUri);
                if (diagnostics == null) return;
                await diagnostics.ContinueWith(t =>
                {
                    if (t.IsCompleted)
                    {
                        languageServer.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams()
                        {
                            Diagnostics = new Container<Diagnostic>(t.Result),
                            Uri = document.DocumentUri,
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Incremental assessment failed with error: ");
            }
        }
    }
}
