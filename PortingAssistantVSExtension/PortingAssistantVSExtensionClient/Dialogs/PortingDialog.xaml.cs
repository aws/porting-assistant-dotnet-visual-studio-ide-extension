using Microsoft.VisualStudio.PlatformUI;
using PortingAssistantVSExtensionClient.Models;
using PortingAssistantVSExtensionClient.Options;

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
            this.ApplyPortActionCheck.IsChecked = _userSettings.ApplyPortAction;
            string title = $"Port selected project or solution to {_userSettings.TargetFramework}";
            this.Title.Content = title;
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
            _userSettings.SaveAllSettings();
            ClickResult = true;
            Close();
        }
    }
}
