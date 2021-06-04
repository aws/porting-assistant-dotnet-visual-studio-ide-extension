using Microsoft.VisualStudio.PlatformUI;
using PortingAssistantVSExtensionClient.Options;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;

namespace PortingAssistantVSExtensionClient.Dialogs
{
    /// <summary>
    /// Interaction logic for ViewDeploymentDialog.xaml
    /// </summary>
    /// 
    public class DeploymentDetail
    {
        public string DeployName { get; set; }
        public string Status { get; set; }
        public string CreationTime { get; set; }
        public Uri DeploymentEndpoint { get; set; }
    }

    public class EndpointConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                string deploymentStack = value.ToString();
                return deploymentStack;
            }
            else
            {
                string deploymentStack = "";
                return deploymentStack;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Uri deploymentStack = new Uri((string)value);
            return deploymentStack;
        }
    }

    public partial class ViewDeploymentDialog : DialogWindow
    {
        private readonly UserSettings _userSettings;
        public ViewDeploymentDialog()
        {
            InitializeComponent();
            _userSettings = UserSettings.Instance;
            this.Title = "View Deployment";
        }

        public static void Execute()
        {
            ViewDeploymentDialog viewDeploymentDialog = new ViewDeploymentDialog();
            ObservableCollection<DeploymentDetail> list = new ObservableCollection<DeploymentDetail>();
            foreach (var result in viewDeploymentDialog._userSettings.DeploymentResults)
            {
                list.Add(result.Value);
            }
            viewDeploymentDialog.DataContext = list;
            viewDeploymentDialog.ShowModal();
        }

        private void View_Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink link = e.OriginalSource as Hyperlink;
            System.Diagnostics.Process.Start(link.NavigateUri.OriginalString);
        }
    }
}
