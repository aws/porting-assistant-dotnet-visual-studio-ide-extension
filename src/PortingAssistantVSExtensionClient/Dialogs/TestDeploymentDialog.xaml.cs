using Microsoft.VisualStudio.PlatformUI;
using PortingAssistantVSExtensionClient.Common;
using PortingAssistantVSExtensionClient.Options;
using PortingAssistantVSExtensionClient.Utils;
using System;
using System.Management.Automation;
using System.IO;
using System.Collections.ObjectModel;
using System.Windows.Documents;

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
        }

        public static string EnsureExecute(string scriptPath, string setupScriptPath)
        {
            TestDeploymentDialog testDeploymenttDialog = new TestDeploymentDialog(scriptPath, setupScriptPath);
            testDeploymenttDialog.ShowModal();
            return testDeploymenttDialog.ClickResult;
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ClickResult = "";
            Close();
        }

        private void Button_Click_1(object sender, System.Windows.RoutedEventArgs e)
        {
            string arg1 = InputBox1.Text;
            string arg2 = InputBox2.Text;
            try
            {
                DeployButton.IsEnabled = false;
                runScript(setupScriptPath);
                runScript(scriptPath);

            }
            catch (Exception ex)
            {
                ClickResult = ex.Message;
            }
            finally
            {
                DeployButton.IsEnabled = true;
            }
        }

        private void runScript(string filePath)
        {
            if (File.Exists(filePath))
            {


                PowerShell powerShell = PowerShell.Create();
                string scriptText = File.ReadAllText(filePath);
                powerShell.AddScript(scriptText);
                var results = powerShell.Invoke();
                if (results.Count > 0)
                {
                    logs.Visibility = System.Windows.Visibility.Visible;
                }
                foreach (PSObject outputItem in results)
                {
                    // if null object was dumped to the pipeline during the script then a null object may be present here
                    if (outputItem != null)
                    {
                        logHolder.Inlines.Add(new Run($"{outputItem}" + System.Environment.NewLine));
                    }
                }


                // check the other output streams (for example, the error stream)
                if (powerShell.Streams.Error.Count > 0)
                {
                    // error records were written to the error stream.
                    // Do something with the error
                }

            }
        }

        private void CheckBox_Checked(object sender, System.Windows.RoutedEventArgs e)
        {

                DbSecretArnInput.Visibility = System.Windows.Visibility.Visible;
                DbSecretArnLabel.Visibility = System.Windows.Visibility.Visible;
                SqlSgLabel.Visibility = System.Windows.Visibility.Visible;
                SqlSgInput.Visibility = System.Windows.Visibility.Visible;
                SqlConnectionLabel.Visibility = System.Windows.Visibility.Visible;
                SqlConnectionInput.Visibility = System.Windows.Visibility.Visible;

                
            

        }

        private void CheckBox_Checked_1(object sender, System.Windows.RoutedEventArgs e)
        {

            ADInput.Visibility = System.Windows.Visibility.Visible;
            ADLabel.Visibility = System.Windows.Visibility.Visible;
            
        }

        private void DBConfigure_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
                DbSecretArnInput.Visibility = System.Windows.Visibility.Hidden;
                DbSecretArnLabel.Visibility = System.Windows.Visibility.Hidden;
                SqlSgLabel.Visibility = System.Windows.Visibility.Hidden;
                SqlSgInput.Visibility = System.Windows.Visibility.Hidden;
                SqlConnectionLabel.Visibility = System.Windows.Visibility.Hidden;
                SqlConnectionInput.Visibility = System.Windows.Visibility.Hidden;
        }

        private void ADConfigure_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            ADInput.Visibility = System.Windows.Visibility.Hidden;
            ADLabel.Visibility = System.Windows.Visibility.Hidden;
        }
    }
}
