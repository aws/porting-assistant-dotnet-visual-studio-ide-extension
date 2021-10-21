using Microsoft.VisualStudio.PlatformUI;
using PortingAssistantVSExtensionClient.Common;
using PortingAssistantVSExtensionClient.Options;
using PortingAssistantVSExtensionClient.Utils;
using System;

using System.IO;
using System.Collections.ObjectModel;
using System.Windows.Documents;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using PortingAssistantVSExtensionClient.Models;

namespace PortingAssistantVSExtensionClient.Dialogs
{
    /// <summary>
    /// Interaction logic for TestDeploymentDialog.xaml
    /// </summary>
    public partial class TestDeploymentDialog : DialogWindow
    {
        public DeploymentParameters parameters { set; get; }
        private readonly UserSettings _userSettings;

        private static TestDeploymentDialog Instance;
        public TestDeploymentDialog()
        {
            InitializeComponent();
            _userSettings = UserSettings.Instance;
            parameters = new DeploymentParameters();
            this.Title = "Test Deployment";
        }

        public static DeploymentParameters GetParameters()
        {
            TestDeploymentDialog testDeploymenttDialog = GetInstance();
            testDeploymenttDialog.ShowModal();
            return testDeploymenttDialog.parameters;
        }

        private static TestDeploymentDialog GetInstance()
        {
            if (Instance == null)
            {
                Instance = new TestDeploymentDialog();
            }
            return Instance;
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Content = "deploying....";
            Close();
        }

        private void AdvanceButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            AdvanceButton.Visibility = System.Windows.Visibility.Hidden;
            GoBackButton.Visibility = System.Windows.Visibility.Visible;
            AdSettingGroup.Visibility = System.Windows.Visibility.Visible;
        }

        private void GoBackButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            AdvanceButton.Visibility = System.Windows.Visibility.Visible;
            GoBackButton.Visibility = System.Windows.Visibility.Hidden;
            AdSettingGroup.Visibility = System.Windows.Visibility.Hidden;
        }

        private void Button_Click_2(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void Browse_Folder_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            FolderBrowserDialog openFolderDialog = new FolderBrowserDialog();

            if (openFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                LocalFolderPathText.Text = openFolderDialog.SelectedPath;
            }
            parameters.buildFolderPath = LocalFolderPathText.Text;

            var oldProfileName = _userSettings.DeploymentProfileName;
            parameters.initDeploymentTool = oldProfileName == "current_profile" ? false : true;
            parameters.enableMetrics = _userSettings.EnabledMetrics;
        }
    }
}
