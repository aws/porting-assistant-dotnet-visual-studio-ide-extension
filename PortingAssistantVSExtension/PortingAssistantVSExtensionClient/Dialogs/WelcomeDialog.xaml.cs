using Microsoft.VisualStudio.PlatformUI;
using PortingAssistantVSExtensionClient.Common;
using PortingAssistantVSExtensionClient.Options;
using System;
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
            InitializeComponent();
            var logoPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), Constants.ResourceFolder, Constants.LogoName);
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(logoPath);
            bitmap.EndInit();
            IconHolder.Source = bitmap;
            CustomerEmail.Text = "";
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _userSettings.EnabledMetric = AgreeToShare.IsChecked ?? false;
            _userSettings.CustomerEmail = CustomerEmail.Text;
            _userSettings.ShowWelcomePage = false;
            _userSettings.SaveAllSettings();
            Close();
        }
    }
}
