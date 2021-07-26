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
            try
            {
                if (File.Exists(scriptPath))
                {


                    PowerShell powerShell = PowerShell.Create();
                    string script = File.ReadAllText(scriptPath);
                    powerShell.AddScript(script);
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
                            Console.WriteLine($"Output line: [{outputItem}]");
                            logHolder.Inlines.Add(new Run($"Output line: [{outputItem}]"));
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
            catch (Exception ex)
            {
                ClickResult = ex.Message;
            }
        }
    }
}
