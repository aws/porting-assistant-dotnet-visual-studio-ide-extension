using PortingAssistantVSExtensionClient.Common;
using PortingAssistantVSExtensionClient.Utils;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PortingAssistantVSExtensionClient.Options
{
    /// <summary>
    /// Interaction logic for OptionPageControl.xaml
    /// </summary>
    public partial class OptionPageControl : UserControl
    {
        public OptionPageControl()
        {
            InitializeComponent();
        }

        private void OnDotnetSupportedVersions(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(ExternalUrls.DotNetSupportedVersions);
        }
    }
}
