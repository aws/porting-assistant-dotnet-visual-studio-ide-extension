using Amazon.Runtime;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using PortingAssistantVSExtensionClient.Common;
using PortingAssistantVSExtensionClient.Models;
using PortingAssistantVSExtensionClient.Options;
using PortingAssistantVSExtensionClient.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace PortingAssistantVSExtensionClient.Dialogs
{
    public partial class WelcomeDialog : DialogWindow
    {
        private readonly UserSettings _userSettings;
        public bool ClickResult = false;
        private readonly string AssemblyPath;
        private readonly string ConfigurationFileName;
        private readonly string ConfigurationPath;
        private TelemetryConfiguration TelemetryConfiguration;

        public WelcomeDialog()
        {
            _userSettings = UserSettings.Instance;
            InitializeComponent();
            initializeUI();
            InitalizeNamedProfile(_userSettings.AWSProfileName);
            this.Title = "Get started";

            this.AssemblyPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            this.ConfigurationFileName = Environment.GetEnvironmentVariable("ConfigurationJson") ?? Common.Constants.DefaultConfigurationFile;
            this.ConfigurationPath = System.IO.Path.Combine(
                AssemblyPath,
                Common.Constants.ResourceFolder,
                ConfigurationFileName);
        }

        private void initializeUI()
        {
            var logoPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), Constants.ResourceFolder, Constants.LogoName);
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(logoPath);
            bitmap.EndInit();
            IconHolder.Source = bitmap;
        }

        public static bool EnsureExecute()
        {
            WelcomeDialog welcomeDialog = new WelcomeDialog();
            welcomeDialog.ShowModal();
            return welcomeDialog.ClickResult;
        }

        private void InitalizeNamedProfile(string newAddedProfile)
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

            ThreadHelper.JoinableTaskFactory.Run(async delegate {
                if (WarningBarDefaultCreds != null)
                {
                    WarningBarDefaultCreds.Content = "Validating Default AWS Credentials.";
                }
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

        private async Task<string> ValidateSDKCredentialsAsync()
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
                    return "Success:" + awsCredential.AwsAccessKeyId;
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
            if (Profiles != null && (Profiles.SelectedItem == null || Profiles.SelectedItem.Equals("")))
            {
                WarningBar.Content = "Profile is required";
            }
            if (WarningBarDefaultCreds != null)
            {
                WarningBarDefaultCreds.Content = "";
            }    
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (AWSProfileSelect.IsChecked != null &&
               (bool)AWSProfileSelect.IsChecked)
            {
                if (Profiles.SelectedItem == null || Profiles.SelectedItem.Equals(""))
                {
                    WarningBar.Content = "Profile is required";
                    return;
                }
            }
            else
            {
                if (WarningBarDefaultCreds != null && !WarningBarDefaultCreds.Content.ToString().Contains("AWS Access Key Id:"))
                {
                    return;
                }
            }
            _userSettings.EnabledMetrics = AgreeToShare.IsChecked ?? false;
            _userSettings.ShowWelcomePage = false;
            if (AWSProfileSelect.IsChecked != null &&
                (bool)AWSProfileSelect.IsChecked)
            {
                _userSettings.AWSProfileName = (string)Profiles.SelectedValue;
                _userSettings.EnabledDefaultCredentials = true;
            }
            else
            {
                _userSettings.AWSProfileName = "DEFAULT_PROFILE";
                _userSettings.EnabledDefaultCredentials = true;
            }
            _userSettings.AWSProfileName = (string)Profiles.SelectedValue;
            _userSettings.SaveAllSettings();
            PortingAssistantLanguageClient.UpdateUserSettingsAsync();
            ClickResult = true;
            Close();
        }

        private void Button_Click_1(object sender, System.Windows.RoutedEventArgs e)
        {
            string newAddedProfile = AddProfileDialog.EnsureExecute();
            if (!newAddedProfile.Equals(""))
            {
                InitalizeNamedProfile(newAddedProfile);
            }
        }

        private void Hyperlink_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(ExternalUrls.Documentation);

        }

        private void Hyperlink_Click_1(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(ExternalUrls.CollectInfomation);
        }

        private void Profiles_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            WarningBar.Content = "";
        }

        private void Hyperlink_Click_2(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(ExternalUrls.Agreement);
        }

        private void Hyperlink_Click_3(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(ExternalUrls.ServiceTerms);
        }

        private void Button_Click_2(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }
    }
}
