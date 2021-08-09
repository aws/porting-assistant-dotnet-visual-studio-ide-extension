using Microsoft.VisualStudio.PlatformUI;
using PortingAssistantVSExtensionClient.Common;
using PortingAssistantVSExtensionClient.Options;
using PortingAssistantVSExtensionClient.Utils;
using System;
using System.Management.Automation;
using System.IO;
using System.Collections.ObjectModel;
using System.Windows.Documents;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;

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
            DeploymentType.Items.Add("EC2 Instance");
            DeploymentType.Items.Add("ECR");
            DeploymentType.SelectedValue = "EC2 Instance";
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
            ClickResult = "";
            Close();
        }

        private async void Button_Click_1(object sender, System.Windows.RoutedEventArgs e)
        {
            string arg1 = InputBox1.Text;
            string arg2 = InputBox2.Text;
            try
            {
                DeployButton.Content = "Deploying";
                DeployButton.IsEnabled = false;
                logHolder.Inlines.Add(new Run("Start deploying the stack in background....." ));
                logHolder.Inlines.Add(new LineBreak());
                logHolder.Inlines.Add(new Run("You can close this window now."));
                //ExecuteCommandSync(scriptPath);
            }
            catch (Exception ex)
            {
                ClickResult = ex.Message;
            }
            finally
            {
                
            }
        }

        public  void ExecuteCommandSync(object command)
        {
            try
            {
                // create the ProcessStartInfo using "cmd" as the program to be run,
                // and "/c " as the parameters.
                // Incidentally, /c tells cmd that we want it to execute the command that follows,
                // and then exit.
                var procStartInfo =
                    new System.Diagnostics.ProcessStartInfo("cmd", "/c " + command);


                // The following commands are needed to redirect the standard output.
                // This means that it will be redirected to the Process.StandardOutput StreamReader.
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                // Do not create the black window.
                procStartInfo.CreateNoWindow = false;
                // Now we create a process, assign its ProcessStartInfo and start it
                var proc = new System.Diagnostics.Process();
                proc.OutputDataReceived += (s, e) => { Console.WriteLine($"{e.Data}"); };
                proc.StartInfo = procStartInfo;
                proc.Start();
                proc.BeginOutputReadLine();
                proc.WaitForExit();
            }
            catch (Exception objException)
            {
                Console.WriteLine("Error: " + objException.Message);
                // Log the exception
            }
        }
        private void DBConfigure_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if(SqlSgLabel != null && SqlSgLabel.Visibility == System.Windows.Visibility.Visible)
            {
                SqlSgLabel.Visibility = System.Windows.Visibility.Hidden;
                SqlSgInput.Visibility = System.Windows.Visibility.Hidden;
                SqlConnectionLabel.Visibility = System.Windows.Visibility.Hidden;
                SqlConnectionInput.Visibility = System.Windows.Visibility.Hidden;
            }   
        }

        private void ADConfigure_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if(ADInput != null && ADInput.Visibility == System.Windows.Visibility.Visible)
            {
                ADInput.Visibility = System.Windows.Visibility.Hidden;
                ADLabel.Visibility = System.Windows.Visibility.Hidden;
                DirectoryName.Visibility = System.Windows.Visibility.Hidden;
                DirectoryNameLabel.Visibility = System.Windows.Visibility.Hidden;
                SecrteLabel.Visibility = System.Windows.Visibility.Hidden;
                SecrteInput.Visibility = System.Windows.Visibility.Hidden;
                PasswordRotation.Visibility = System.Windows.Visibility.Hidden;
            }
            
        }

        private void ADConfigure_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if(ADInput!= null && ADInput.Visibility == System.Windows.Visibility.Hidden)
            {
                ADInput.Visibility = System.Windows.Visibility.Visible;
                ADLabel.Visibility = System.Windows.Visibility.Visible;
                DirectoryName.Visibility = System.Windows.Visibility.Visible;
                DirectoryNameLabel.Visibility = System.Windows.Visibility.Visible;
                SecrteLabel.Visibility = System.Windows.Visibility.Visible;
                SecrteInput.Visibility = System.Windows.Visibility.Visible;
                PasswordRotation.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void DBConfigure_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (SqlSgLabel != null && SqlSgLabel.Visibility == System.Windows.Visibility.Hidden)
            {
                SqlSgLabel.Visibility = System.Windows.Visibility.Visible;
                SqlSgInput.Visibility = System.Windows.Visibility.Visible;
                SqlConnectionLabel.Visibility = System.Windows.Visibility.Visible;
                SqlConnectionInput.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void IISConfigure_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (ApplicationPools != null && ApplicationPools.Visibility == System.Windows.Visibility.Hidden)
            {
                ApplicationPools.Visibility = System.Windows.Visibility.Visible;
                ApplicationPoolsLabel.Visibility = System.Windows.Visibility.Visible;
                ApplicationPoolsID.Visibility = System.Windows.Visibility.Visible;
                ApplicationPoolsIDLabel.Visibility = System.Windows.Visibility.Visible;
                IISCertificate.Visibility = System.Windows.Visibility.Visible;
                IISCertificateLabel.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void IISConfigure_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (ApplicationPools != null && ApplicationPools.Visibility == System.Windows.Visibility.Visible)
            {
                ApplicationPools.Visibility = System.Windows.Visibility.Hidden;
                ApplicationPoolsLabel.Visibility = System.Windows.Visibility.Hidden;
                ApplicationPoolsID.Visibility = System.Windows.Visibility.Hidden;
                ApplicationPoolsIDLabel.Visibility = System.Windows.Visibility.Hidden;
                IISCertificate.Visibility = System.Windows.Visibility.Hidden;
                IISCertificateLabel.Visibility = System.Windows.Visibility.Hidden;
            }
        }
    }
}
