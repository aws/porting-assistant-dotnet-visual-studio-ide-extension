using System.ComponentModel.Composition;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;

namespace PortingAssistantVSExtensionClient.ContentDefinitions
{
    class CsharpContentDefinition
    {
        [Export]
        [Name("csharp")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
        internal static ContentTypeDefinition CsContentTypeDefinition;



        [Export]
        [FileExtension(".cs")]
        [ContentType("csharp")]
        internal static FileExtensionToContentTypeDefinition CsFileExtensionDefinition;
    }
}
