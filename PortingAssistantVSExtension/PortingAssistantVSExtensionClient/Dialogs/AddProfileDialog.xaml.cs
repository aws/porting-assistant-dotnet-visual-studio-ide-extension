using Microsoft.VisualStudio.PlatformUI;
using PortingAssistantVSExtensionClient.Common;
using System;
namespace PortingAssistantVSExtensionClient.Dialogs
{
    /// <summary>
    /// Interaction logic for AddProfileDialog.xaml
    /// </summary>
    public partial class AddProfileDialog : DialogWindow
    {
        public string ClickResult = "";
        public AddProfileDialog()
        {
            InitializeComponent();
            this.Title = "Add a named profile";
        }

        public static string EnsureExecute()
        {
            AddProfileDialog AddProfileDialog = new AddProfileDialog();
            AddProfileDialog.ShowModal();
            return AddProfileDialog.ClickResult;
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(ProfileName.Text))
            {
                WarningProfileName.Content = "Please enter the AWS profile name!";
                return;
            }
            else
            {
                WarningProfileName.Content = "";
            }
            if (String.IsNullOrEmpty(AccesskeyID.Text)) {
                WarningAccessKeyID.Content = "Please enter the AWS Access Key ID!";
                return;
            }
            else
            {
                WarningAccessKeyID.Content = "";
            }
            if (String.IsNullOrEmpty(secretAccessKey.Text))
            {
                WarningSecretKey.Content = "Please enter the AWS Secret Access Key!";
                return;
            }
            else
            {
                WarningSecretKey.Content = "";
            }
            PAGlobalService.Instance.SaveProfile(ProfileName.Text, new AwsCredential(AccesskeyID.Text, secretAccessKey.Text));
            ClickResult = ProfileName.Text;
            Close();
        }

        private void Button_Click_1(object sender, System.Windows.RoutedEventArgs e)
        {
            ClickResult = "";
            Close();
        }
    }
}
