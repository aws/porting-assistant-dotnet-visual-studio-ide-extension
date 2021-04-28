using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistantVSExtensionClient.Common
{
    public static class Constants
    {
        public const string ApplicationName = "Porting Assistant for .NET";

        public const string ApplicationServerLocation = "PortingAssistantLanguageServer";

        public const string ApplicationServerName = "PortingAssistantExtensionServer.exe";

        public const string OptionName = "General";

        public const string DataOptionName = "Data usage sharing";

        public const string DebugInPipeName = "extensionclientreadpipe";

        public const string DebugOutPipeName = "extensionclientwritepipe";

        public const string InPipeName = "paclientreadpipe";

        public const string OutPipeName = "paclientwritepipe";

        public const string LogoName = "PortingAssistantLogo.png";

        public const string DefaultConfigurationFile = "porting-assistant-config-dev.json";

        public const string ResourceFolder = "Resources";
    }
}
