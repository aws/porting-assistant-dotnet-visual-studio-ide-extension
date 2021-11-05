using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistantVSExtensionClient.Utils
{
    public class FilesUtils
    {
        public static string GetTmpFolder()
        {
            var tmpFolder = Path.Combine(Path.GetTempPath(), "PortingAssistant", Path.GetRandomFileName());
            if (!Directory.Exists(tmpFolder)) Directory.CreateDirectory(tmpFolder);
            return tmpFolder;
        }
    }
}
