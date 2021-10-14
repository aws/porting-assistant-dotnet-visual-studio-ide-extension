using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
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
        public Amazon.RegionEndpoint Region;

        public AwsCredential(string AwsAccessKeyId, string AwsSecretKey)
        {
            this.AwsAccessKeyId = AwsAccessKeyId;
            this.AwsSecretKey = AwsSecretKey;
            this.Region = Amazon.RegionEndpoint.USEast1;
        }
    }

    public static class AwsUtils
    {
        private static readonly SharedCredentialsFile sharedProfile = new SharedCredentialsFile();
        public static List<string> ListProfiles()
        {
            return sharedProfile.ListProfileNames();
        }

        public static AWSCredentials GetAWSCredentials(string profileName)
        {
            if (sharedProfile.TryGetProfile(profileName, out var basicProfile) &&
                AWSCredentialsFactory.TryGetAWSCredentials(basicProfile, sharedProfile, out var awsCredentials))
            {
                return awsCredentials;
            }
            return null;
        }

        public static async Task CreateDefaultBucketAsync(String profileName, String bucketName)
        {
            AWSCredentials credentials = GetAWSCredentials(profileName);
            using (var s3Client = new AmazonS3Client(credentials))
            {
                if (!await AmazonS3Util.DoesS3BucketExistV2Async(s3Client, bucketName))
                {
                    var putBucketRequest = new PutBucketRequest
                    {
                        BucketName = bucketName,
                        UseClientRegion = true
                    };

                    PutBucketResponse putBucketResponse = await s3Client.PutBucketAsync(putBucketRequest);
                }
            }
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
            AwsCredential credential)
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
            return errors;
        }

        public async static Task<string> ValidateProfile(
            string profileName,
            AwsCredential credential,
            TelemetryConfiguration telemetryConfiguration)
        {
            if (!await VerifyUserAsync(profileName, credential, telemetryConfiguration))
            {
                return "Please provide a valid aws profile";
            }
            else
            {
                SaveProfile(profileName, credential);
                return "";
            }
        }

        private static async Task<bool> VerifyUserAsync
            (
            string profile,
            AwsCredential awsCredentials,
            TelemetryConfiguration telemetryConfiguration
            )
        {
            const string PathTemplate = "/put-log-data";
            try
            {
                var client = new HttpClient();
                var profileName = profile;
                var region = telemetryConfiguration.Region;
                var signer = new AWS4RequestSigner
                    (
                    awsCredentials.AwsAccessKeyId,
                    awsCredentials.AwsSecretKey
                    );

                dynamic requestMetadata = new JObject();
                requestMetadata.version = "1.0";
                requestMetadata.service = telemetryConfiguration.ServiceName;
                requestMetadata.token = "12345678";
                requestMetadata.description = telemetryConfiguration.Description;

                dynamic log = new JObject();
                log.timestamp = DateTime.Now.ToString();
                log.logName = "verify-user-vs";
                log.logData = "";
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
                return response.StatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
