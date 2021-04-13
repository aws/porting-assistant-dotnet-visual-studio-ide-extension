using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Serialization;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using PortingAssistantExtensionServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PortingAssistantExtensionServer.Handlers
{
    class PortingAssistantCodeActionHandler : ICodeActionHandler
    {
        private readonly PortingAssistantTextSyncHandler _portingAssistantTextSyncHandler;
        private readonly ILanguageServerFacade _languageServer;
        private readonly SolutionAnalysisService _solutionAnalysisService;
        private readonly CodeActionRegistrationOptions _registrationOptions; 
        
        private CodeActionCapability _capability;
        public void SetCapability(CodeActionCapability capability)
        {
            capability.DataSupport = true;
            _capability = capability;
        }

        public CodeActionRegistrationOptions GetRegistrationOptions()
        {
            return _registrationOptions;
        }


        public PortingAssistantCodeActionHandler(PortingAssistantTextSyncHandler portingAssistantTextSyncHandler,
            ILanguageServerFacade languageServer, SolutionAnalysisService solutionAnalysisService)
        {
            _portingAssistantTextSyncHandler = portingAssistantTextSyncHandler;
            _languageServer = languageServer;
            _solutionAnalysisService = solutionAnalysisService;
            _registrationOptions = new CodeActionRegistrationOptions
            {
                DocumentSelector = _portingAssistantTextSyncHandler.GetRegistrationOptions().DocumentSelector,
                CodeActionKinds = new Container<CodeActionKind>(
                CodeActionKind.Empty,
                CodeActionKind.QuickFix,
                CodeActionKind.Refactor,
                CodeActionKind.RefactorExtract,
                CodeActionKind.RefactorInline,
                CodeActionKind.RefactorRewrite,
                CodeActionKind.Source,
                CodeActionKind.SourceOrganizeImports
            )
            };
        }

        public async Task<CommandOrCodeActionContainer> Handle(CodeActionParams request, CancellationToken cancellationToken)
        {
            var codeActions = new List<CommandOrCodeAction>();
            await Task.Run(() =>
            {
                foreach (var diagnostic in request.Context.Diagnostics)
                {
                    var diagnosticHash = _solutionAnalysisService.HashDiagnostic(diagnostic, request.TextDocument.Uri.Path);

                    if (_solutionAnalysisService.CodeActions.ContainsKey(diagnosticHash))
                    {
                        //Selecting changes for this file only
                        var textEdits = _solutionAnalysisService.CodeActions[diagnosticHash]
                            .Where(t => _solutionAnalysisService.TrimFilePath(t.FileLinePositionSpan.Path) == _solutionAnalysisService.TrimFilePath(request.TextDocument.Uri.Path))
                            .Select(t => new TextEdit() { NewText = t.NewText, 
                                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                                    t.FileLinePositionSpan.StartLinePosition.Line,
                                    t.FileLinePositionSpan.StartLinePosition.Character,
                                    t.FileLinePositionSpan.EndLinePosition.Line,
                                    t.FileLinePositionSpan.EndLinePosition.Character)
                                    });
                            ;
                        
                        codeActions.Add(new CodeAction
                        {
                            Title = diagnostic.Message,
                            Kind = CodeActionKind.QuickFix,
                            Diagnostics = new List<Diagnostic>() { diagnostic },
                            Edit = new WorkspaceEdit
                            {
                                DocumentChanges = new Container<WorkspaceEditDocumentChange>(
                    new WorkspaceEditDocumentChange(
                        new TextDocumentEdit
                        {
                            TextDocument = new VersionedTextDocumentIdentifier
                            {
                                Uri = request.TextDocument.Uri
                            },
                            Edits = new TextEditContainer(
                                            textEdits
                                )
                        }))
                            }
                        });
                    }
                }
            });
            return codeActions;
        }

    }
}
