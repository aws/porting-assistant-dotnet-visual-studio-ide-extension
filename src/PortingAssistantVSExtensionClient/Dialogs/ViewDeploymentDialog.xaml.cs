using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Collections.ObjectModel;
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
        public ViewDeploymentDialog()
        {
            InitializeComponent();
            this.Title = "View Deployment";
            DeploymentDetail detail = new DeploymentDetail()
            {
                DeployName = "test",
                Status = "Done",
                CreationTime = "2021-10-21:00:00:00",
                DeploymentEndpoint = new Uri("https://us-west-2.console.aws.amazon.com/cloudformation/home?region=us-west-2")

            };
            ObservableCollection<DeploymentDetail> testList = new ObservableCollection<DeploymentDetail>() { detail };
            DeploymentTable.DataContext = testList;
        }

        public static void Execute()
        {
            ViewDeploymentDialog viewDeploymentDialog = new ViewDeploymentDialog();
            viewDeploymentDialog.ShowModal();
        }

        private void View_Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink link = e.OriginalSource as Hyperlink;
            System.Diagnostics.Process.Start(link.NavigateUri.OriginalString);
        }
    }
}
