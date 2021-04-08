using System.ComponentModel.Composition;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;

namespace PortingAssistantVSExtensionClient.ContentDefinitions
{
    class CsharpContentDefinition
    {
        [Export]
        [Name("CSharpFileType")]
        [BaseDefinition("CSharp")]
        internal static ContentTypeDefinition CsContentTypeDefinition;



        [Export]
        [FileExtension(".cs")]
        [ContentType("CSharpFileType")]
        internal static FileExtensionToContentTypeDefinition CsFileExtensionDefinition;
    }
}
