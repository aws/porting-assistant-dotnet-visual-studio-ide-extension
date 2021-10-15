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
        public static string GetInitJsonFilePath(string bucketName, bool enableMetrics, string tmpFolder)
        {
            var path = Path.Combine(tmpFolder, "init.json");
            JObject jObject = new JObject(
                new JProperty("S3BucketName", bucketName),
                new JProperty("metrics", enableMetrics)
                );

            File.WriteAllText(path, jObject.ToString());
            return path;
        }

        public static string GetTmpFolder()
        {
            var tmpFolder = Path.Combine(Path.GetTempPath(), "PortingAssistant", Path.GetRandomFileName());
            if (!Directory.Exists(tmpFolder)) Directory.CreateDirectory(tmpFolder);
            return tmpFolder;
        }
    }
}
