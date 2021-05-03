using Microsoft.VisualStudio.PlatformUI;
using PortingAssistantVSExtensionClient.Common;
using PortingAssistantVSExtensionClient.Options;
using PortingAssistantVSExtensionClient.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace PortingAssistantVSExtensionClient.Dialogs
{
    public partial class WelcomeDialog : DialogWindow
    {
        private readonly UserSettings _userSettings;
        public bool ClickResult = false;
        public WelcomeDialog()
        {
            _userSettings = UserSettings.Instance;
            InitializeComponent();
            initializeUI();
            InitalizeNamedProfile(_userSettings.AWSProfileName);
            this.Title = "Get started";
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

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if(Profiles.SelectedItem == null || Profiles.SelectedItem.Equals(""))
            {
                WarningBar.Content = "Profile is required";
                return;
            }
            _userSettings.EnabledMetrics = AgreeToShare.IsChecked ?? false;
            _userSettings.ShowWelcomePage = false;
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
