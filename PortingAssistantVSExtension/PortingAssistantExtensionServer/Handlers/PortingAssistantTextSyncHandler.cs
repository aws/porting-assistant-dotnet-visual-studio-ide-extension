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


        private ImmutableDictionary<DocumentUri, SemanticTokensDocument> _tokenDocuments = ImmutableDictionary<DocumentUri, SemanticTokensDocument>.Empty.WithComparers(DocumentUri.Comparer);



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



        public bool TryGetTokenDocument(DocumentUri documentUri, SemanticTokensRegistrationOptions options, out SemanticTokensDocument document)

        {

            if (_solutionAnalysisService._openDocuments.TryGetValue(documentUri, out _))

            {

                if (!_tokenDocuments.TryGetValue(documentUri, out document))

                    _tokenDocuments = _tokenDocuments.Add(documentUri, document = new SemanticTokensDocument(options));

                return true;

            }

            document = null;

            return false;

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



            //var actions = Program.SolutionResult?.ProjectResults?.FirstOrDefault().ProjectActions?

            //    .FileActions?.FirstOrDefault(f => ComparePaths(f.FilePath, request.TextDocument.Uri.Path));



            //var diagnostics = new List<Diagnostic>();



            //foreach (var nodeToken in actions?.NodeTokens)

            //{

            //    var diagnostic = new Diagnostic()

            //    {

            //        Range = new Range(

            //            new Position((int)nodeToken.TextSpan.StartLinePosition - 1, (int)nodeToken.TextSpan.StartCharPosition - 1),

            //            new Position((int)nodeToken.TextSpan.EndLinePosition - 1, (int)nodeToken.TextSpan.EndCharPosition - 1)),

            //        Message = nodeToken.Description

            //    };

            //    diagnostics.Add(diagnostic);

            //}



            //languageServer.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams()

            //{

            //    Diagnostics = new Container<Diagnostic>(diagnostics),

            //    Uri = value.DocumentUri,

            //    Version = value.Version

            //});

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

            if (_solutionAnalysisService._openDocuments.TryGetValue(request.TextDocument.Uri, out var value))

            {

                value.Load(request.Text);



                languageServer.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams()

                {

                    Diagnostics = new Container<Diagnostic>(value.GetDiagnostics()),

                    Uri = value.DocumentUri,

                    Version = value.Version

                });

            }



            return Unit.Task;

        }



        public void SetCapability(SynchronizationCapability capability) { _capability = capability; }

        public TextDocumentChangeRegistrationOptions GetRegistrationOptions() { return _options; }



        TextDocumentRegistrationOptions IRegistration<TextDocumentRegistrationOptions>.GetRegistrationOptions() { return _options; }



        TextDocumentSaveRegistrationOptions IRegistration<TextDocumentSaveRegistrationOptions>.GetRegistrationOptions() { return _saveOptions; }

    }
}
