using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;

namespace PortingAssistantExtensionServer.TextDocumentModels
{
    class CodeSection
    {
        private string name;
        private string fullName;
        private Range textRange;
        private Dictionary<string, Diagnostic> diagnostics;
        private Dictionary<string, string> codeActions;

    }
}
