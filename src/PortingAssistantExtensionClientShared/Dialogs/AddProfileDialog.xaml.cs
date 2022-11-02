using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;
using PortingAssistantVSExtensionClient.Common;
using PortingAssistantVSExtensionClient.Models;
using PortingAssistantVSExtensionClient.Utils;
using System;
using System.Collections.Generic;
using System.IO;

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
            this.Title = "Add a Named Profile";
        }

        public static string EnsureExecute()
        {
            AddProfileDialog AddProfileDialog = new AddProfileDialog();
            AddProfileDialog.ShowModal();
            return AddProfileDialog.ClickResult;
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {

            var errors = new Dictionary<string, string>();
            AwsCredential credential;
            if(String.IsNullOrEmpty(sessionToken.Text))
            {
                credential = new AwsCredential(AccesskeyID.Text, secretAccessKey.Text);
            }
            else
            {
                credential = new AwsCredential(AccesskeyID.Text, secretAccessKey.Text, sessionToken.Text);
            }
            try
            {
                errors = AwsUtils.ValidateProfile(ProfileName.Text, credential);
                if (errors.TryGetValue("profile", out string error1))
                {
                    WarningProfileName.Content = error1;
                }
                else
                {
                    WarningProfileName.Content = "";
                }
                if (errors.TryGetValue("accessKeyId", out string error2))
                {
                    WarningAccessKeyID.Content = error2;
                }
                else
                {
                    WarningAccessKeyID.Content = "";
                }
                if (errors.TryGetValue("secretKey", out string error3))
                {
                    WarningSecretKey.Content = error3;
                }
                else
                {
                    WarningSecretKey.Content = "";
                }
                if (errors.Count == 0)
                {
                    WarningValidation.Content = "validating AWS profile, please wait";
                    var task = AwsUtils.ValidateProfile(
                        ProfileName.Text,
                        credential,
                        PortingAssistantLanguageClient.Instance.TelemetryConfiguration);

                    ThreadHelper.JoinableTaskFactory.Run(async delegate
                    {
                        var result = await AwsUtils.ValidateProfile(
                            ProfileName.Text,
                            credential,
                            PortingAssistantLanguageClient.Instance.TelemetryConfiguration);

                        WarningValidation.Content = result;
                    });
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            if (errors.Count == 0 && WarningValidation.Content.Equals(""))
            {
                ClickResult = ProfileName.Text;
                Close();
            }
            else return;
            
        }

        private void Button_Click_1(object sender, System.Windows.RoutedEventArgs e)
        {
            ClickResult = "";
            Close();
        }

        private void Hyperlink_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(ExternalUrls.ConfigAWSCLI);
        }
    }
}
