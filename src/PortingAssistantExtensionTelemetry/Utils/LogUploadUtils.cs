using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PortingAssistantExtensionTelemetry.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using PortingAssistant.Client.Telemetry;
using Amazon;

namespace PortingAssistantExtensionTelemetry.Utils
{
    public static class LogUploadUtils
    {
        private static bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open
                (
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite
                );
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }
            return false;
        }

        private static async Task<bool> PutLogData
            (
            string logName,
            string logData,
            string profile,
            bool enabledDefaultCredentials,
            TelemetryConfiguration telemetryConfiguration
            )
        {
            try
            {
                var chain = new CredentialProfileStoreChain();
                AWSCredentials awsCredentials;

                if (enabledDefaultCredentials)
                {
                    awsCredentials = FallbackCredentialsFactory.GetCredentials();
                    if (awsCredentials == null)
                    {
                        Console.WriteLine("Invalid Credentials.");
                        return false;
                    }
                }
                else
                {
                    var profileName = profile;
                    if (!chain.TryGetAWSCredentials(profileName, out awsCredentials))
                    {
                        Console.WriteLine("Invalid Credentials.");
                        return false;
                    }
                }

                var region = telemetryConfiguration.Region;
                dynamic requestMetadata = new JObject();
                requestMetadata.version = "1.0";
                requestMetadata.service = telemetryConfiguration.ServiceName;
                requestMetadata.token = "12345678";
                requestMetadata.description = telemetryConfiguration.Description;

                dynamic log = new JObject();
                log.timestamp = DateTime.Now.ToString();
                log.logName = logName;
                var logDataInBytes = Encoding.UTF8.GetBytes(logData);
                log.logData = Convert.ToBase64String(logDataInBytes);

                dynamic body = new JObject();
                body.requestMetadata = requestMetadata;
                body.log = log;

                var requestContent = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json");
                var config = new TelemetryConfig()
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(region),
                    MaxErrorRetry = 2,
                    ServiceURL = telemetryConfiguration.InvokeUrl,
                };
                var client = new TelemetryClient(awsCredentials, config);
                var contentString = await requestContent.ReadAsStringAsync();
                var telemetryRequest = new TelemetryRequest(telemetryConfiguration.ServiceName, contentString);
                var telemetryResponse = await client.SendAsync(telemetryRequest);
                return telemetryResponse.HttpStatusCode == HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public static void OnTimedEvent(object source, System.Timers.ElapsedEventArgs e, bool shareMetric, TelemetryConfiguration teleConfig, string lastReadTokenFile, string profile, bool enabledDefaultCredentials)
        {
            try
            {
                if (!shareMetric) return;
                // Get files in directory and filter based on Suffix
                string[] fileEntries = Directory.GetFiles(teleConfig.LogsPath).Where(f =>
                  teleConfig.Suffix.ToArray().Any(x => f.EndsWith(x))
                  ).ToArray();
                // Get or Create fileLineNumberMap
                var fileLineNumberMap = new Dictionary<string, int>();
                if (File.Exists(lastReadTokenFile))
                {
                    fileLineNumberMap = JsonConvert.DeserializeObject<Dictionary<string, int>>(File.ReadAllText(lastReadTokenFile));
                }
                var initLineNumber = 0;
                foreach (var file in fileEntries)
                {
                    var fName = Path.GetFileNameWithoutExtension(file);
                    var fileExtension = Path.GetExtension(file);
                    var logName = "";
                    // Check which type of log file and set the prefix
                    if (fileExtension == ".metrics")
                    {
                        logName = "portingAssistant-ide-metrics";
                    }
                    else if (fileExtension == ".log")
                    {
                        logName = "portingAssistant-ide-logs";
                    }
                    else
                    {
                        continue;
                    }

                    var logs = new ArrayList();

                    // Add new files to fileLineNumberMap
                    if (!fileLineNumberMap.ContainsKey(file))
                    {
                        fileLineNumberMap[file] = 0;
                    }
                    initLineNumber = fileLineNumberMap[file];
                    FileInfo fileInfo = new FileInfo(file);
                    var success = false;
                    if (!IsFileLocked(fileInfo))
                    {
                        using (FileStream fs = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            using (StreamReader reader = new StreamReader(fs))
                            {
                                string line = null;
                                int currLineNumber = 0;
                                for (; currLineNumber < initLineNumber; currLineNumber++)
                                {
                                    line = reader.ReadLine();
                                    if (line == null)
                                    {
                                        return;
                                    }
                                }

                                line = reader.ReadLine();

                                // If put-log api works keep sending logs else wait and do it next time
                                while (line != null && logs.Count <= 1000)
                                {
                                    currLineNumber++;
                                    logs.Add(line);
                                    line = reader.ReadLine();

                                    // send 1000 lines of logs each time when there are large files
                                    if (logs.Count >= 1000)
                                    {
                                        // logs.TrimToSize();
                                        success = PutLogData(logName, JsonConvert.SerializeObject(logs), profile, enabledDefaultCredentials, teleConfig).Result;
                                        if (success) { logs = new ArrayList(); };
                                    }
                                }

                                if (logs.Count != 0)
                                {
                                    success = PutLogData(logName, JsonConvert.SerializeObject(logs), profile, enabledDefaultCredentials, teleConfig).Result;
                                }
                                if (success)
                                {
                                    fileLineNumberMap[file] = currLineNumber;
                                    string jsonString = JsonConvert.SerializeObject(fileLineNumberMap);
                                    File.WriteAllText(lastReadTokenFile, jsonString);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}