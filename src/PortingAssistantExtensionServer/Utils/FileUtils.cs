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

        public static Boolean download(String url, String location)
        {
            try
            {
                using (WebClient myWebClient = new WebClient())
                {
                    myWebClient.DownloadFile(url, location);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
