using Microsoft.VisualStudio.PlatformUI;
using PortingAssistantVSExtensionClient.Common;
using PortingAssistantVSExtensionClient.Options;
using PortingAssistantVSExtensionClient.Utils;
using System;
using System.Diagnostics;
using System.IO;

namespace PortingAssistantVSExtensionClient.Dialogs
{
    /// <summary>
    /// Interaction logic for TestDeploymentDialog.xaml
    /// </summary>
    public partial class TestDeploymentDialog : DialogWindow
    {
        public string ClickResult = "";

        private string scriptPath = "";
        public TestDeploymentDialog(string scriptPath)
        {
            InitializeComponent();
            this.scriptPath = scriptPath;
        }

        public static string EnsureExecute(string scriptPath)
        {
            TestDeploymentDialog testDeploymenttDialog = new TestDeploymentDialog(scriptPath);
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
            if (File.Exists(scriptPath))
            {
                try
                {
                    ProcessStartInfo info = new ProcessStartInfo()
                    {
                        FileName = "powershell.exe",
                        WorkingDirectory = Path.GetDirectoryName(scriptPath),
                        UseShellExecute = false,
                        CreateNoWindow = false,
                        Arguments = $"-NoProfile -ExecutionPolicy unrestricted -file \"{scriptPath}\"",
                    };
                    Process process = new Process { StartInfo = info };
                    if (process.Start())
                    {
                        ClickResult = "success";
                    }
                }
                catch (Exception ex)
                {
                    ClickResult = ex.Message;
                }
            }
            
            Close();
        }
    }
}
