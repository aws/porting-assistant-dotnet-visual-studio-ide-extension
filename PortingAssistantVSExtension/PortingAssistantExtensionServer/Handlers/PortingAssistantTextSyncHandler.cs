using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol;
using MediatR;
using System.Threading;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using System.Linq;
using System.Collections.Immutable;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Document.Proposals;
using OmniSharp.Extensions.LanguageServer.Protocol.Models.Proposals;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using PortingAssistantExtensionServer.TextDocumentModels;

namespace PortingAssistantExtensionServer.Handlers
{
    class PortingAssistantTextSyncHandler : ITextDocumentSyncHandler
    {

        private readonly TextDocumentChangeRegistrationOptions _options;
        private readonly TextDocumentSaveRegistrationOptions _saveOptions;
        private readonly ILanguageServerFacade languageServer;
        private readonly SolutionAnalysisService _solutionAnalysisService;
        private SynchronizationCapability _capability;

        public void SetCapability(SynchronizationCapability capability) { _capability = capability; }
        public TextDocumentChangeRegistrationOptions GetRegistrationOptions() { return _options; }
        TextDocumentRegistrationOptions IRegistration<TextDocumentRegistrationOptions>.GetRegistrationOptions() { return _options; }
        TextDocumentSaveRegistrationOptions IRegistration<TextDocumentSaveRegistrationOptions>.GetRegistrationOptions() { return _saveOptions; }

        public PortingAssistantTextSyncHandler(ILanguageServerFacade languageServer, SolutionAnalysisService solutionAnalysisService)
        {
            _options = new TextDocumentChangeRegistrationOptions()
            {
                DocumentSelector = new DocumentSelector(DocumentSelector.ForPattern("**/*.cs").Concat(DocumentSelector.ForScheme("csharp"))),
                SyncKind = TextDocumentSyncKind.Incremental
            };
            _saveOptions = new TextDocumentSaveRegistrationOptions()
            {
                DocumentSelector = _options.DocumentSelector,
                IncludeText = true
            };
            this.languageServer = languageServer;
            this._solutionAnalysisService = solutionAnalysisService;
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
                return new TextDocumentAttributes(uri, "csharp");
            }
            return null;
        }



        public Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
        {
            if (!_solutionAnalysisService._openDocuments.TryGetValue(request.TextDocument.Uri, out var value)) return Unit.Task;
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
            lock (_solutionAnalysisService._openDocuments)
            {
                var document = new CodeFileDocument(request.TextDocument.Uri);
                _solutionAnalysisService._openDocuments = _solutionAnalysisService._openDocuments.Add(request.TextDocument.Uri, document);
                document.Load(request.TextDocument.Text);
                if (_solutionAnalysisService.HasSolutionAnalysisResult())
                {
                    var diagnostics = _solutionAnalysisService.GetDiagnostics(document.DocumentUri);
                    languageServer.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams()
                    {
                        Diagnostics = new Container<Diagnostic>(diagnostics),
                        Uri = document.DocumentUri,
                    });
                }
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
            if (!_capability.DidSave) return Unit.Task;
            if (_solutionAnalysisService._openDocuments.TryGetValue(request.TextDocument.Uri, out var document))
            {
                document.Load(request.Text);
                if (_solutionAnalysisService.HasSolutionAnalysisResult())
                {
                    var diagnostics = _solutionAnalysisService.GetDiagnostics(document.DocumentUri);
                    languageServer.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams()
                    {
                        Diagnostics = new Container<Diagnostic>(diagnostics),
                        Uri = document.DocumentUri,
                    });
                }
            }
            return Unit.Task;
        }

        

    }
}
