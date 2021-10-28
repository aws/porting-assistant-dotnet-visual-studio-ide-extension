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
        public static string GetInitJsonFilePath(string bucketName, bool enableMetrics, string tmpFolder, string profileName)
        {
            var path = Path.Combine(tmpFolder, "init.json");
            var workspace = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Local", "app2container");
            JObject jObject = new JObject(
                new JProperty("a2CTemplateVersion", "1.0"),
                new JProperty("workspace", workspace),
                new JProperty("useInstanceProfile", false),
                new JProperty("profileRegion", ""),
                new JProperty("awsProfile", profileName),
                new JProperty("s3Bucket", bucketName),
                new JProperty("metricsReportPermission", enableMetrics),
                new JProperty("supportBundleUploadPermission", true),
                new JProperty("dockerContentTrust", false));

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
