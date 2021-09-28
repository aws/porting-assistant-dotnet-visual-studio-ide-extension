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

namespace PortingAssistantVSExtensionClient.Dialogs
{
    /// <summary>
    /// Interaction logic for TestDeploymentDialog.xaml
    /// </summary>
    public partial class TestDeploymentDialog : DialogWindow
    {
        public string ClickResult = "";

        private string scriptPath = "";

        private string setupScriptPath = "";
        public TestDeploymentDialog(string scriptPath, string setupScriptPath)
        {
            InitializeComponent();
            this.scriptPath = scriptPath;
            this.setupScriptPath = setupScriptPath;
            this.Title = "Test Deployment";
        }

        public static string EnsureExecute(string scriptPath, string setupScriptPath)
        {
            TestDeploymentDialog testDeploymenttDialog = new TestDeploymentDialog(scriptPath, setupScriptPath);
            testDeploymenttDialog.ShowModal();
            return testDeploymenttDialog.ClickResult;
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Content = "deploying....";
        }

        private void Button_Click_1(object sender, System.Windows.RoutedEventArgs e)
        {
            FolderBrowserDialog openFolderDialog = new FolderBrowserDialog();

            if (openFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                BuildFolderPath.Text = openFolderDialog.SelectedPath;
            }
        }
    }
}
