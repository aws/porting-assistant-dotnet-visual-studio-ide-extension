using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using PortingAssistantVSExtensionClient.Dialogs;
using PortingAssistantVSExtensionClient.Models;
using PortingAssistantVSExtensionClient.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace PortingAssistantVSExtensionClient.Options
{
    /// <summary>
    /// Interaction logic for DataSharingOptionPage.xaml
    /// </summary>
    public partial class DataSharingOptionPage : UserControl
    {

        private readonly string AssemblyPath;
        private readonly string ConfigurationFileName;
        private readonly string ConfigurationPath;
        private TelemetryConfiguration TelemetryConfiguration;
        public DataSharingOptionPage()
        {
            InitializeComponent();

            this.AssemblyPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            this.ConfigurationFileName = Environment.GetEnvironmentVariable("ConfigurationJson") ?? Common.Constants.DefaultConfigurationFile;
            this.ConfigurationPath = System.IO.Path.Combine(
                AssemblyPath,
                Common.Constants.ResourceFolder,
                ConfigurationFileName);

        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        public void InitalizeNamedProfile(string newAddedProfile)
        {
            List<string> namedProfiles = AwsUtils.ListProfiles();
            if (namedProfiles != null && namedProfiles.Count != 0)
            {
                Profiles.Items.Clear();
                foreach (var namedProfile in namedProfiles)
                {
                    Profiles.Items.Add(namedProfile);
                }
            }
            Profiles.SelectedItem = newAddedProfile;
        }

        private void SDKChain_Checked(Object sender, System.Windows.RoutedEventArgs e)
        {
            if (AwsProfileLabel != null && AwsProfileLabel.IsEnabled == true)
            {
                AwsProfileLabel.IsEnabled = false;
            }
            if (Profiles != null && Profiles.IsEnabled == true)
            {
                Profiles.IsEnabled = false;
            }
            if (AddProfileButton != null && AddProfileButton.IsEnabled == true)
            {
                AddProfileButton.IsEnabled = false;
            }
            if (WarningBar != null)
            {
                WarningBar.Content = "";
            }
            if (WarningBarDefaultCreds != null)
            {
                WarningBarDefaultCreds.Content = "Validating Default AWS Credentials.";
            }

            ThreadHelper.JoinableTaskFactory.Run(async delegate {
                var result = await ValidateSDKCredentialsAsync();
                if (result.Contains("Success"))
                {
                    WarningBarDefaultCreds.Content = "AWS Access Key Id: " + result.Split(':')[1];
                    return;
                }
                else
                {
                    if (result == "Default Credentials not Found in any provider." || result == "Default Credentials do not have valid permissions.")
                    {
                        WarningBarDefaultCreds.Content = result;
                    }
                }
            });
        }

        public async Task<string> ValidateAWSProfileAsync(string profile)
        {
            var chain = new CredentialProfileStoreChain();
            AWSCredentials awsCredentials;
            if (!chain.TryGetAWSCredentials(profile, out awsCredentials))
            {
                return "No Credentials found for profile";
            }
            else
            {
                var immutableCredentials = await awsCredentials.GetCredentialsAsync();
                AwsCredential awsCredential = new AwsCredential(immutableCredentials.AccessKey, immutableCredentials.SecretKey, immutableCredentials.Token);
                this.TelemetryConfiguration = JsonConvert.DeserializeObject<PortingAssistantIDEConfiguration>(File.ReadAllText(this.ConfigurationPath)).TelemetryConfiguration;

                if (await AwsUtils.VerifyUserAsync(profile, awsCredential, this.TelemetryConfiguration))
                {
                    return "Success";
                }
                else
                {
                    return "Credentials asosciated with profile do not have valid permissions.";
                }
            }

        }

        public async Task<string> ValidateSDKCredentialsAsync()
        {
            var credentials = FallbackCredentialsFactory.GetCredentials();
            if (credentials == null)
            {
                return "Default Credentials not Found in any provider.";
            }
            else
            {
                var immutableCredentials = await credentials.GetCredentialsAsync();
                AwsCredential awsCredential = new AwsCredential(immutableCredentials.AccessKey, immutableCredentials.SecretKey, immutableCredentials.Token);
                this.TelemetryConfiguration = JsonConvert.DeserializeObject<PortingAssistantIDEConfiguration>(File.ReadAllText(this.ConfigurationPath)).TelemetryConfiguration;

                if (await AwsUtils.VerifyUserAsync("", awsCredential, this.TelemetryConfiguration))
                {
                    return "Success:" + awsCredential.AwsAccessKeyId.ToString();
                }
                else
                {
                    return "Default Credentials do not have valid permissions.";
                }
            }
        }

        private void AWSProfile_Checked(Object sender, System.Windows.RoutedEventArgs e)
        {
            if (AwsProfileLabel != null && AwsProfileLabel.IsEnabled == false)
            {
                AwsProfileLabel.IsEnabled = true;
            }
            if (Profiles != null && Profiles.IsEnabled == false)
            {
                Profiles.IsEnabled = true;
            }
            if (AddProfileButton != null && AddProfileButton.IsEnabled == false)
            {
                AddProfileButton.IsEnabled = true;
            }
            if (WarningBarDefaultCreds != null)
            {
                WarningBarDefaultCreds.Content = "";
            }
            if (Profiles != null && (Profiles.SelectedItem == null || Profiles.SelectedItem.Equals("")))
            {
                WarningBar.Content = "Profile is required";
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string newAddedProfile = AddProfileDialog.EnsureExecute();
            if (!newAddedProfile.Equals(""))
            {
                InitalizeNamedProfile(newAddedProfile);
            }
        }

        private void Profiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            WarningBar.Content = "";
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(Common.ExternalUrls.CollectInfomation);
        }
    }
}
