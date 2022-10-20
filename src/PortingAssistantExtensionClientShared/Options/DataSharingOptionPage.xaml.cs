using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using PortingAssistantVSExtensionClient.Dialogs;
using PortingAssistantVSExtensionClient.Models;
using PortingAssistantVSExtensionClient.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace PortingAssistantVSExtensionClient.Options
{
    /// <summary>
    /// Interaction logic for DataSharingOptionPage.xaml
    /// </summary>
    public partial class DataSharingOptionPage : UserControl
    {

        private readonly string AssemblyPath;
        private readonly string ConfigurationFileName;
        private readonly string ConfigurationPath;
        private TelemetryConfiguration TelemetryConfiguration;
        public DataSharingOptionPage()
        {
            InitializeComponent();

            this.AssemblyPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            this.ConfigurationFileName = Environment.GetEnvironmentVariable("ConfigurationJson") ?? Common.Constants.DefaultConfigurationFile;
            this.ConfigurationPath = System.IO.Path.Combine(
                AssemblyPath,
                Common.Constants.ResourceFolder,
                ConfigurationFileName);

        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(Common.ExternalUrls.CollectInfomation);
        }

    }
}
