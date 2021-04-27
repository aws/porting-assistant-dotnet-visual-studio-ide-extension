using PortingAssistantVSExtensionClient.Common;
using PortingAssistantVSExtensionClient.Dialogs;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for DataSharingOptionPage.xaml
    /// </summary>
    public partial class DataSharingOptionPage : UserControl
    {
        public DataSharingOptionPage()
        {
            InitializeComponent();
            
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        public void InitalizeNamedProfile(string newAddedProfile)
        {
            List<string> namedProfiles = PAGlobalService.Instance.ListProfiles();
            if (namedProfiles != null && namedProfiles.Count != 0)
            {
                Profiles.Items.Clear();
                foreach (var namedProfile in namedProfiles)
                {
                    Profiles.Items.Add(namedProfile);
                }
            }
            Profiles.SelectedItem = newAddedProfile;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string newAddedProfile = AddProfileDialog.EnsureExecute();
            if (!newAddedProfile.Equals(""))
            {
                InitalizeNamedProfile(newAddedProfile);
            }
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Hyperlink_Click_1(object sender, RoutedEventArgs e)
        {

        }
    }
}
