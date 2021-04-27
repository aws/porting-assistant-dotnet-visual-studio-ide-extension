using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            ClearCache.IsEnabled = false;
            ClearCache.Foreground = Brushes.Gray;
        }
    }
}
