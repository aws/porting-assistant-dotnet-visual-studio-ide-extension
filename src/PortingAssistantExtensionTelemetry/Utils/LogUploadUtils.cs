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
using Serilog;

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

        private static AWSCredentials GetAWSCredentials(string profile, bool enabledDefaultCredentials)
        {
            var chain = new CredentialProfileStoreChain();
            AWSCredentials awsCredentials;

            if (enabledDefaultCredentials)
            {
                awsCredentials = FallbackCredentialsFactory.GetCredentials();
                if (awsCredentials == null)
                {
                    return null;
                }
            }
            else
            {
                var profileName = profile;
                if (!chain.TryGetAWSCredentials(profileName, out awsCredentials))
                {
                    return null;
                }
            }

            return awsCredentials;
        }

        private static async Task<bool> PutLogData(
            string logName,
            string logData,
            TelemetryConfiguration telemetryConfiguration,
            AWSCredentials awsCredentials,
            ILogger logger)
        {
            try
            {

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
                var config = new TelemetryClientConfig()
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
                logger.Error("Log Upload Failed: " + ex.Message);
                return false;
            }
        }

        public static void OnTimedEvent(
            object source,
            System.Timers.ElapsedEventArgs e,
            bool shareMetric,
            TelemetryConfiguration teleConfig,
            string lastReadTokenFile,
            string profile,
            bool enabledDefaultCredentials,
            string paVersion,
            ILogger logger)
        {
            try
            {
                if (!shareMetric) return;

                AWSCredentials awsCredentials = GetAWSCredentials(profile, enabledDefaultCredentials);
                if (awsCredentials == null)
                {
                    logger.Error("Log Upload Failed. Could not retrieve any AWS Credentials");
                    return;
                }

                // Get files in directory and filter based on Suffix
                string[] fileEntries = Directory
                    .GetFiles(teleConfig.LogsPath)
                    .Where(f =>
                        teleConfig.Suffix.ToArray().Any(x => f.EndsWith(x)))
                    .ToArray();

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

                    // Add new files to fileLineNumberMap
                    if (!fileLineNumberMap.ContainsKey(file))
                    {
                        fileLineNumberMap[file] = 0;
                    }
                    initLineNumber = fileLineNumberMap[file];
                    FileInfo fileInfo = new(file);
                    if (IsFileLocked(fileInfo))
                    {
                        continue;
                    }

                    using (FileStream fs = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (StreamReader reader = new(fs))
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

                            // According to Amazon API Gateway, HTTP API payload size is capped at 10MB.
                            // https://docs.aws.amazon.com/apigateway/latest/developerguide/limits.html
                            // Sometimes the log line is large and total of 1000 lines will exceed that limit.
                            // Use the payload size instead of line count to resolve that constrain.
                            // We don't want to use all 10MB for the payload, set the limit to 2MB for each log upload.
                            const int uploadLogBatchMaxSize = 2097152; // (Math.Pow(2, 20) * 10 / 5) = 2MB.
                            var success = false;
                            long currentBatchPayloadSize = 0;
                            line = reader.ReadLine();
                            var logs = new ArrayList();
                            // If put-log api works keep sending logs else wait and do it next time
                            while (line != null && currentBatchPayloadSize <= uploadLogBatchMaxSize)
                            {
                                currLineNumber++;
                                // Add previously read line into log.
                                logs.Add(line);
                                currentBatchPayloadSize += line.Length * sizeof(char);

                                // Read next line and try to add into same payload logs.
                                line = reader.ReadLine();
                                if (line == null)
                                {
                                    break;
                                }

                                // Estimate if adding current line's size will exceed the 2MB cap,
                                // Upload previously accumulated logs if so.
                                if ((currentBatchPayloadSize + line.Length * sizeof(char)) >= uploadLogBatchMaxSize)
                                {
                                    success = PutLogData(
                                        logName,
                                        JsonConvert.SerializeObject(logs),
                                        teleConfig,
                                        awsCredentials,
                                        logger)
                                        .Result;

                                    // If upload succeeded, reset logs and currentBatchPayloadSize for next iteration.
                                    if (success)
                                    {
                                        logs = new ArrayList();
                                        currentBatchPayloadSize = 0;
                                    }
                                    else // Upload faile then exit the while loop, and wait for next upload timer event.
                                    {
                                        break;
                                    }
                                }
                            }

                            // Try to upload if log size is smaller than 2MB, or retry the last failed upload before exit.
                            if (logs.Count != 0)
                            {
                                success = PutLogData(
                                    logName,
                                    JsonConvert.SerializeObject(logs),
                                    teleConfig,
                                    awsCredentials,
                                    logger)
                                    .Result;
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
            catch (Exception ex)
            {
                logger.Error("Log Upload Failed: " + ex.Message);
            }
        }
    }
}
