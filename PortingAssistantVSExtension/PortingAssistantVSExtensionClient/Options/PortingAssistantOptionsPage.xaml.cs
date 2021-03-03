using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Drawing;
using System.Windows.Controls;

namespace PortingAssistantVSExtensionClient.Options
{
    /// <summary>
    /// Interaction logic for PortingAssistantOptionsPage.xaml
    /// </summary>
    public partial class PortingAssistantOptionsPage : UserControl
    {
        private readonly  PortingAssistantOptions optionsPage;
        public PortingAssistantOptionsPage(PortingAssistantOptions optionPage)
        {
            InitializeComponent();
            this.optionsPage = optionPage;
        }

        

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
