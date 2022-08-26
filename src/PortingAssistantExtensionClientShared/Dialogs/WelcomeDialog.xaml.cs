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

 


        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _userSettings.EnabledMetrics = AgreeToShare.IsChecked ?? false;
            _userSettings.ShowWelcomePage = true;
            _userSettings.SaveAllSettings();
            PortingAssistantLanguageClient.UpdateUserSettingsAsync();
            ClickResult = true;
            Close();
        }


        private void Hyperlink_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(ExternalUrls.Documentation);

        }

        private void Hyperlink_Click_1(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(ExternalUrls.CollectInfomation);
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
