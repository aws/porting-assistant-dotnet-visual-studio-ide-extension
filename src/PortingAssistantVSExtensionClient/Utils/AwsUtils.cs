using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Aws4RequestSigner;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PortingAssistantVSExtensionClient.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistantVSExtensionClient.Utils
{
    public class AwsCredential
    {
        public string AwsAccessKeyId;
        public string AwsSecretKey;

        public AwsCredential(string AwsAccessKeyId, string AwsSecretKey)
        {
            this.AwsAccessKeyId = AwsAccessKeyId;
            this.AwsSecretKey = AwsSecretKey;
        }
    }

    public static class AwsUtils
    {
        private static readonly SharedCredentialsFile sharedProfile = new SharedCredentialsFile();
        public static List<string> ListProfiles()
        {
            return sharedProfile.ListProfileNames();
        }


        public static void SaveProfile(string profileName, AwsCredential credential)
        {
            try
            {
                var profile = new CredentialProfile(
                                name: profileName,
                                profileOptions: new CredentialProfileOptions
                                {
                                    AccessKey = credential.AwsAccessKeyId,
                                    SecretKey = credential.AwsSecretKey
                                });
                profile.Region = Amazon.RegionEndpoint.USEast1;
                sharedProfile.RegisterProfile(profile);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            
        }

        public static Dictionary<string, string> ValidateProfile(
            string profileName, 
            AwsCredential credential, 
            TelemetryConfiguration telemetryConfiguration)
        {
            Dictionary<string, string> errors = new Dictionary<string, string>();
            if (String.IsNullOrEmpty(profileName))
            {
                errors.Add("profile", "Enter the AWS profile name");
            }
            if (String.IsNullOrEmpty(credential.AwsAccessKeyId))
            {
                errors.Add("accessKeyId", "Enter the AWS Access Key ID");
            }
            if (String.IsNullOrEmpty(credential.AwsSecretKey))
            {
                errors.Add("secretKey", "Enter the AWS Secret Access Key");
            }
            if (errors.Count == 0)
            {
                SaveProfile(profileName, credential);
                //var verifyUserTask = VerifyUserAsync(profileName, telemetryConfiguration);
                //verifyUserTask.Wait();
                //if (!verifyUserTask.Result)
                //{
                //    errors.Add("validation", "Please provide a valid aws profile");
                //}
            }
            return errors;
        }


        private static async Task<bool> VerifyUserAsync
            (
            string profile,
            TelemetryConfiguration telemetryConfiguration
            )
        {
            const string PathTemplate = "/put-log-data";
            try
            {

                var client = new HttpClient();
                var chain = new CredentialProfileStoreChain();
                AWSCredentials awsCredentials;
                var profileName = profile;
                var region = telemetryConfiguration.Region;
                if (chain.TryGetAWSCredentials(profileName, out awsCredentials))
                {
                    var signer = new AWS4RequestSigner
                        (
                        awsCredentials.GetCredentials().AccessKey,
                        awsCredentials.GetCredentials().SecretKey
                        );

                    dynamic requestMetadata = new JObject();
                    requestMetadata.version = "1.0";
                    requestMetadata.service = telemetryConfiguration.ServiceName;
                    requestMetadata.token = "12345678";
                    requestMetadata.description = telemetryConfiguration.Description;

                    dynamic log = new JObject();
                    log.timestamp = DateTime.Now.ToString();
                    log.logName = "verify-user-vs";
                    var logDataInBytes = System.Text.Encoding.UTF8.GetBytes("");
                    log.logData = System.Convert.ToBase64String(logDataInBytes);

                    dynamic body = new JObject();
                    body.requestMetadata = requestMetadata;
                    body.log = log;

                    var requestContent = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json");

                    var requestUri = new Uri(String.Join("", telemetryConfiguration.InvokeUrl, PathTemplate));

                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = requestUri,
                        Content = requestContent
                    };

                    request = await signer.Sign(request, "execute-api", region);

                    var response = await client.SendAsync(request);

                    return response.StatusCode==System.Net.HttpStatusCode.OK;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
