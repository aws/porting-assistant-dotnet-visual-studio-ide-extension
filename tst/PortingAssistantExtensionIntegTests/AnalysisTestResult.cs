using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace PortingAssistantExtensionIntegTests
{
    public class JsonUtils
    {
        public static string ToJson(AnalysisTestResult result)
        {
            return JsonConvert.SerializeObject(result);
        }

        public static void ToJsonFile(AnalysisTestResult result, string filePath)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;

            using (StreamWriter sw = new StreamWriter(filePath))
            using (JsonWriter writer = new JsonTextWriter(sw) { Formatting = Formatting.Indented })
            {
                serializer.Serialize(writer, result);
            }
        }

        public static AnalysisTestResult FromJsonFile(string path)
        {

            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;

            using (StreamReader sw = new StreamReader(path))
            using (JsonReader reader = new JsonTextReader(sw))
            {
                return serializer.Deserialize<AnalysisTestResult>(reader);
            }
        }

        public static string FromJson(string result)
        {
            return JsonConvert.SerializeObject(result);
        }

    }

    public class AnalysisTestResult
    {
        public Dictionary<string, List<CompatEntry>> FileCompatResults;
        public AnalysisTestResult()
        {
            FileCompatResults = new Dictionary<string, List<CompatEntry>>();
        }

        public void AddEntry(CompatEntry entry)
        {
            List<CompatEntry> entriesList = null;
            if (!FileCompatResults.ContainsKey(entry.fileName))
            {
                entriesList = new List<CompatEntry>();
                FileCompatResults[entry.fileName] = entriesList;
            } 
            else
            {
                entriesList = FileCompatResults[entry.fileName];
            }
            entriesList.Add(entry);
        }

        public ISet<string> GetCompatResultsAsSet()
        {
            ISet<string> set = new HashSet<string>();
            foreach(var fileList in FileCompatResults.Values)
            {
                fileList.ForEach(e => set.Add(e.ToString()));
            }

            return set;
        }
    }


    public class CompatEntry
    {
        public CompatEntry(String fname, String code, String msg, Range range)
        {
            fileName = fname;
            compatCode = code;
            message = msg;
            Range = range;
        }

        public CompatEntry()
        {

        }

        public string fileName;
        public string compatCode;
        public string message;
        public Range Range;

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(fileName).Append("#");
            sb.Append(compatCode).Append("#");
            sb.Append(message).Append("#");
            sb.Append(Range);
            return sb.ToString();
        }
    }
}
