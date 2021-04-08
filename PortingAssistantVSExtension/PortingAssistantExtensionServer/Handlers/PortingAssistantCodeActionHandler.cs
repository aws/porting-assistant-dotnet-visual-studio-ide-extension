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
            await Task.Run(() => {
                foreach (var diagnostic in request.Context.Diagnostics)
                { 
                    codeActions.Add(new CodeAction
                    {
                        Title = "Test code action",
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
                                        new TextEdit()
                                        {
                                            NewText = "",
                                            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range((1, 0), (1, 3))
                                        }
                            )
                    }))
                        },
                        //Command = Command.Create("fix-whitespace")
                        //    .WithArguments(
                        //        new Location()
                        //        {
                        //            Range = request.Range,
                        //            Uri = request.TextDocument.Uri
                        //        }
                        //    )
                    });
                }
            });
            return codeActions;
        }

    }
}
