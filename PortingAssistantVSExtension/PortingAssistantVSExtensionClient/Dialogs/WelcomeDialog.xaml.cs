using Microsoft.VisualStudio.PlatformUI;
using PortingAssistantVSExtensionClient.Common;
using PortingAssistantVSExtensionClient.Options;
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
        public WelcomeDialog()
        {
            _userSettings = UserSettings.Instance;
            InitializeComponent();
            initializeUI();
            InitalizeNamedProfile(_userSettings.AWSProfileName);
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

        private void InitalizeNamedProfile(string newAddedProfile)
        {
            List<string> namedProfiles = PAGlobalService.Instance.ListProfiles();
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
            _userSettings.EnabledMetrics = AgreeToShare.IsChecked ?? false;
            _userSettings.ShowWelcomePage = false;
            _userSettings.AWSProfileName = (string)Profiles.SelectedValue;
            _userSettings.SaveAllSettings();
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
    }
}
