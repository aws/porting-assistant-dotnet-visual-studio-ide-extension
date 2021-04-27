using Microsoft.VisualStudio.PlatformUI;
using PortingAssistantVSExtensionClient.Common;
using PortingAssistantVSExtensionClient.Models;
using PortingAssistantVSExtensionClient.Options;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace PortingAssistantVSExtensionClient.Dialogs
{
    public partial class PortingDialog : DialogWindow
    {
        private readonly UserSettings _userSettings;

        public bool ClickResult = false;
        public PortingDialog()
        {
            _userSettings = UserSettings.Instance;
            InitializeComponent();
            var logoPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), Constants.ResourceFolder, "StatusInformation.png");
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(logoPath);
            bitmap.EndInit();
            InfoSign.Source = bitmap;
            this.ApplyPortActionCheck.IsChecked = _userSettings.ApplyPortAction;
            this.Title = $"Port selected project or solution to {_userSettings.TargetFramework}";
        }

        public static bool EnsureExecute()
        {
            PortingDialog portingDialog = new PortingDialog();
            portingDialog.ShowModal();
            return portingDialog.ClickResult;
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ClickResult = false;
            Close();
        }

        private void Button_Click_1(object sender, System.Windows.RoutedEventArgs e)
        {
            _userSettings.ApplyPortAction = ApplyPortActionCheck.IsChecked ?? false;
            _userSettings.UpdateApplyPortAction();
            ClickResult = true;
            Close();
        }
    }
}
