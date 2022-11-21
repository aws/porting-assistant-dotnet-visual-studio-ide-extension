using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Diagnostics;
using Amazon;
using System.Windows;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using PortingAssistantVSExtensionClient.Models;
using Amazon.S3;
using Amazon.S3.Model;
using System.Windows.Controls;

namespace PortingAssistantVSExtensionClient.Utils
{
    public class SupportedVersionsUtil
    {
        #region Singleton
        private SupportedVersionsUtil()
        {
        }

        public static SupportedVersionsUtil Instance { get { return Nested.instance; } }

        private class Nested
        {
            static Nested() { }

            internal static readonly SupportedVersionsUtil instance = new SupportedVersionsUtil();
        }
        #endregion Singleton

        private SupportedVersionConfiguration _supportedVersionConfiguration;
        public SupportedVersionConfiguration SupportedVersionConfiguration
        {
            get
            {
                if (_supportedVersionConfiguration == null)
                {
                    ThreadHelper.JoinableTaskFactory.Run(async () =>
                    {
                        // Reading from public S3, credentials are not needed.
                        using (var s3Client = new AmazonS3Client(RegionEndpoint.GetBySystemName(SupportedVersionConfiguration.S3Region)))
                        {
                            var result = await GetSupportedConfigurationAsync(
                                    s3Client,
                                    SupportedVersionConfiguration.S3BucketName,
                                    SupportedVersionConfiguration.S3File,
                                    SupportedVersionConfiguration.ExpectedBucketOwnerId);

                            _supportedVersionConfiguration = result.Item1;
                            if (!string.IsNullOrEmpty(result.Item2))
                            {
                                Trace.WriteLine(result.Item2);
                            }
                        }
                    });
                }

                return _supportedVersionConfiguration;
            }
        }

        private Version _visualStudioVersion;
        public Version VisualStudioVersion
        {
            get
            {
                if (_visualStudioVersion == null)
                {
                    ThreadHelper.JoinableTaskFactory.Run(async () =>
                    {
                        _visualStudioVersion = await GetVisualStudioVersionAsync();
                    });
                }

                return _visualStudioVersion;
            }
        }


        /// <summary>
        /// This is the official way to get Visual Studio client version.
        /// For more information, visit the VsixCommunity github:
        /// https://github.com/VsixCommunity/Community.VisualStudio.Toolkit/blob/master/demo/VSSDK.TestExtension/ToolWindows/RunnerWindow.cs#L21
        /// </summary>
        /// <returns></returns>
        public async Task<Version> GetVisualStudioVersionAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IVsShell shell = await ServiceProvider.GetGlobalServiceAsync<SVsShell, IVsShell>(swallowExceptions: false);
            shell.GetProperty((int)__VSSPROPID5.VSSPROPID_ReleaseVersion, out object value);

            if (value is string raw)
            {
                return Version.Parse(raw.Split(' ')[0]);
            }

            return null;
        }

        public async Task<(SupportedVersionConfiguration, string)> GetSupportedConfigurationAsync(
            AmazonS3Client s3Client,
            string bucketName,
            string s3File,
            string expectedBucketOwnerId,
            bool showDialog = true)
        {
            SupportedVersionConfiguration result = new SupportedVersionConfiguration();
            string resultStatus = string.Empty;
            try
            {
                GetObjectRequest request = new GetObjectRequest()
                {
                    BucketName = bucketName,
                    Key = s3File,
                    ExpectedBucketOwner = expectedBucketOwnerId,
                };
                var response = await s3Client.GetObjectAsync(request);
                using (var streamReader = new StreamReader(response.ResponseStream))
                {
                    result = JsonConvert.DeserializeObject<SupportedVersionConfiguration>(await streamReader.ReadToEndAsync());
                }

                // Make sure to sort the version items before presenting to the UI.
                result.Versions.Sort();
            }
            catch (AmazonS3Exception s3Exception)
            {
                if (s3Exception.StatusCode != HttpStatusCode.NotFound)
                {
                    resultStatus = $"Porting Assistant failed to read supported versions configuration, fall back to default values. " +
                        $"Please verify your internect connection and restart Visual Studio. \n{s3Exception.Message}";
                }
                else
                {
                    resultStatus = $"The supported version configuration file is not available, fall back to default values. " +
                        $"Please reach out to aws-toolkit-for-net-refactoring-support@amazon.com for support. \n{s3Exception.Message}";
                }
                // Fall back to default as sugguested by OBR.
                result = SupportedVersionConfiguration.GetDefaultConfiguration();
            }
            catch (Exception ex)
            {
                resultStatus = $"Porting Assistant failed to configure supported versions, fall back to default values. " +
                    $"Please verify your internect connection and restart Visual Studio. \n{ex.Message}";
                // Fall back to default as sugguested by OBR.
                result = SupportedVersionConfiguration.GetDefaultConfiguration();
            }

            if (!string.IsNullOrEmpty(resultStatus) && showDialog)
            {
                MessageBox.Show(resultStatus, "Porting Assistant for .NET");
            }

            return (result, resultStatus);
        }

        public void UpdateComboBox(ComboBox frameworkComboBox)
        {
            frameworkComboBox.Items.Clear();
            // Sort based on recommended order.
            if (SupportedVersionConfiguration?.Versions != null)
            {
                foreach (var version in SupportedVersionConfiguration.Versions)
                {
                    if (Version.TryParse(version.RequiredVisualStudioVersion, out Version requiredVSVersion) &&
                        requiredVSVersion <= VisualStudioVersion)
                    {
                        frameworkComboBox.Items.Add(version.DisplayName);
                    }
                }
            }
        }
    }
}

