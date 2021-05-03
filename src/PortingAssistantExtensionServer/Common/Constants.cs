namespace PortingAssistantExtensionServer.Common
{
    public static class Constants
    {
        public const string stdDebugInPipeName = "extensionclientwritepipe";
        public const string stdDebugOutPipeName = "extensionclientreadpipe";
        public const string DefaultOutputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] (Porting Assistant IDE Extension) {SourceContext}: {Message:lj}{NewLine}{Exception}";
        public const string PortingAssitantHelpUrl = "https://aws.amazon.com/porting-assistant-dotnet/";
        public const string DiagnosticSource = "Porting Assistant";
        public const string DiagnosticCode = "PA0001";
        public const string DiagnosticWithActionCode = "PA0002";

    }
}
