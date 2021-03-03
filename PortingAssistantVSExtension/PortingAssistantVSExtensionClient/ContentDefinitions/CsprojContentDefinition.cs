using System.ComponentModel.Composition;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;


namespace PortingAssistantVSExtensionClient.ContentDefinitions
{
    class CsprojContentDefinition
    {
        [Export]
        [Name("csproj")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
        internal static ContentTypeDefinition CsprojContentTypeDefinition;



        [Export]
        [FileExtension(".csproj")]
        [ContentType("XML")]
        internal static FileExtensionToContentTypeDefinition CsprojFileExtensionDefinition;
    }
}
