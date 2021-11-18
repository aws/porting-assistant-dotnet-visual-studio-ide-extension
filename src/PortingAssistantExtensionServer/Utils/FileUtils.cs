using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace PortingAssistantExtensionServer.Utils
{
    public class FileUtils
    {
        public static bool ExistsOnPath(string fileName)
        {
            return GetFullPath(fileName) != null;
        }

        public static string GetFullPath(string fileName)
        {
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);

            var values = Environment.GetEnvironmentVariable("PATH");
            foreach (var path in values.Split(Path.PathSeparator))
            {
                var fullPath = Path.Combine(path, fileName);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            return null;
        }

        public static string GetTmpFolder()
        {
            var tmpFolder = Path.Combine(Path.GetTempPath(), "PortingAssistant", Path.GetRandomFileName());
            if (!Directory.Exists(tmpFolder)) Directory.CreateDirectory(tmpFolder);
            return tmpFolder;
        }

        public static void Download(String url, String location)
        {
            using (WebClient myWebClient = new WebClient())
            {
                myWebClient.DownloadFile(url, location);
            }
        }
    }
}
