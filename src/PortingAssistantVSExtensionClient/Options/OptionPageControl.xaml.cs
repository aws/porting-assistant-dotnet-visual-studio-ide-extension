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
            ClearCache.IsEnabled = true;
            ClearCache.Foreground = Brushes.Blue;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
                string solutionPath = PAGlobalService.DTE2.Value.Solution.FullName;
                var tmpPath = SolutionUtils.GetTempDirectory(solutionPath);
                Directory.Delete(tmpPath, recursive:true);
            }
            catch (Exception)
            {
            }
            finally
            {
                ClearCache.IsEnabled = false;
                ClearCache.Foreground = Brushes.Gray;
            }
        }
    }
}
