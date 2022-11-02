using PortingAssistantVSExtensionClient.Common;
using System.Windows.Controls;

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
