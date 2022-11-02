﻿using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime.CredentialManagement;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PortingAssistantVSExtensionClient.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using PortingAssistantVSExtensionClient.Options;
using PortingAssistantVSExtensionClient.Common;
using PortingAssistantExtensionClientShared.Models;

namespace PortingAssistantVSExtensionClient.Utils
{
    public class AwsCredential
    {
        public string AwsAccessKeyId;
        public string AwsSecretKey;
        public string SessionToken = "";

        public AwsCredential(string AwsAccessKeyId, string AwsSecretKey)
        {
            this.AwsAccessKeyId = AwsAccessKeyId;
            this.AwsSecretKey = AwsSecretKey;
        }

        public AwsCredential(string AwsAccessKeyId, string AwsSecretKey, string SessionToken)
        {
            this.AwsAccessKeyId = AwsAccessKeyId;
            this.AwsSecretKey = AwsSecretKey;
            this.SessionToken = SessionToken;
        }
    }

    public static class AwsUtils
    {
        private static readonly SharedCredentialsFile sharedProfile = new SharedCredentialsFile();
        public static bool IsCredsNotificationDismissed = true;
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
                if (!String.IsNullOrEmpty(credential.SessionToken))
                {
                    profile.Options.Token = credential.SessionToken;
                }
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

        public static async Task<bool> VerifyUserAsync
            (
            string profile,
            AwsCredential awsCredentials,
            TelemetryConfiguration telemetryConfiguration
            )
        {
            try
            {
                var profileName = profile;
                var region = String.IsNullOrEmpty(telemetryConfiguration.Region)?"us-east-1": telemetryConfiguration.Region;
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
                var config = new TelemetryConfig()
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(region),
                    MaxErrorRetry = 2,
                    ServiceURL = telemetryConfiguration.InvokeUrl,
                };
                TelemetryClient client;
                if (String.IsNullOrEmpty(awsCredentials.SessionToken))
                {
                    client = new TelemetryClient(awsCredentials.AwsAccessKeyId, awsCredentials.AwsSecretKey, config);
                }
                else
                {
                    client = new TelemetryClient(awsCredentials.AwsAccessKeyId, awsCredentials.AwsSecretKey, awsCredentials.SessionToken, config);
                }
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

        public static async Task<AwsCredential> GetAwsCredentialsAsync(bool enabledDefaultCredentials, string profileName)
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
                if (!chain.TryGetAWSCredentials(profileName, out awsCredentials))
                {
                    return null;
                }
            }

            var immutableCredentials = await awsCredentials.GetCredentialsAsync();
            AwsCredential awsCredential = new AwsCredential(immutableCredentials.AccessKey, immutableCredentials.SecretKey, immutableCredentials.Token);

            return awsCredential;
        }


        public static async Task<bool> ValidateProfileAsync()
        {
            if (UserSettings.Instance.AWSProfileName != null || UserSettings.Instance.EnabledDefaultCredentials)
            {
                AwsCredential awsCredentials = await GetAwsCredentialsAsync(UserSettings.Instance.EnabledDefaultCredentials, UserSettings.Instance.AWSProfileName);

                var AssemblyPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var ConfigurationFileName = Environment.GetEnvironmentVariable("ConfigurationJson") ?? Common.Constants.DefaultConfigurationFile;
                var ConfigurationPath = System.IO.Path.Combine(
                    AssemblyPath,
                    Common.Constants.ResourceFolder,
                    ConfigurationFileName);
                var TelemetryConfiguration = JsonConvert.DeserializeObject<PortingAssistantIDEConfiguration>(File.ReadAllText(ConfigurationPath)).TelemetryConfiguration;

                if ((awsCredentials == null || !await AwsUtils.VerifyUserAsync("", awsCredentials, TelemetryConfiguration)) &&  AwsUtils.IsCredsNotificationDismissed) 
                {
                    return false;
                }
            }  
            return true;
        }

        public static async Task<SupportedVersionConfiguration> GetSupportedConfigurationAsync()
        {
            SupportedVersionConfiguration result = new SupportedVersionConfiguration();
            try
            {
                using (var s3Client = new AmazonS3Client(RegionEndpoint.GetBySystemName(SupportedVersionConfiguration.S3Region)))
                {
                    GetObjectRequest request = new GetObjectRequest()
                    {
                        BucketName = SupportedVersionConfiguration.S3BucketName,
                        Key = SupportedVersionConfiguration.S3File,
                        ExpectedBucketOwner = SupportedVersionConfiguration.ExpectedBucketOwnerId,
                    };
                    var response = await s3Client.GetObjectAsync(request);
                    using (var streamReader = new StreamReader(response.ResponseStream))
                    {
                        result = JsonConvert.DeserializeObject<SupportedVersionConfiguration>(await streamReader.ReadToEndAsync());
                    }
                }

                // Make sure to sort the version items before presenting to the UI.
                result.Versions.Sort();
            }
            catch (AmazonS3Exception s3Exception)
            {
                if (s3Exception.StatusCode != HttpStatusCode.NotFound)
                {
                    NotificationUtils.ShowErrorMessageBox(
                        PAGlobalService.Instance.Package,
                        $"Porting Assistant failed to configure supported versions. Please verify your internect connection and restart Visual Studio. \n {s3Exception}",
                        "Porting Assistant for .NET");
                }
                else
                {
                    NotificationUtils.ShowErrorMessageBox(
                        PAGlobalService.Instance.Package,
                        "The supported version configuration file is not availabe, please reach out to aws-porting-assistant-support@amazon.com for support.",
                        "Porting Assistant for .NET");
                }
            }
            catch (Exception ex)
            {
                NotificationUtils.ShowErrorMessageBox(
                    PAGlobalService.Instance.Package,
                    $"Porting Assistant failed to configure supported versions. Please verify your internect connection and restart Visual Studio. \n{ex}",
                    "Porting Assistant for .NET");
            }

            return result;
        }
    }
}
