using System;
using System.Collections.Generic;
using System.Text;

namespace PortingAssistantExtensionServer.Common
{
    public static class Constants
    {
        public const string stdDebugInPipeName = "extensionclientwritepipe";
        public const string stdDebugOutPipeName = "extensionclientreadpipe";
        public const string DefaultOutputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";
        public const string PortingAssitantHelpUrl = "https://aws.amazon.com/porting-assistant-dotnet/";
        public const string DiagnosticSource = "Porting Assistant";
        public const string DiagnosticCode = "PA-001";
        public const string DiagnosticWithActionCode = "PA-002";

    }
}
