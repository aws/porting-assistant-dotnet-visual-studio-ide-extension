using Microsoft.VisualStudio.PlatformUI;
using PortingAssistantVSExtensionClient.Common;
using PortingAssistantVSExtensionClient.Options;
using PortingAssistantVSExtensionClient.Utils;
using System;

using System.IO;
using System.Collections.ObjectModel;
using System.Windows.Documents;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using PortingAssistantVSExtensionClient.Models;
using System.Windows.Media;
using System.Linq;
using Amazon;
using System.Data;

namespace PortingAssistantVSExtensionClient.Dialogs
{
    /// <summary>
    /// Interaction logic for ViewDeploymentDialog.xaml
    /// </summary>
    public partial class ViewDeploymentDialog : DialogWindow
    {
        public ViewDeploymentDialog()
        {
            InitializeComponent();
            this.Title = "View Deployment";
        }

        public static void Execute()
        {
            ViewDeploymentDialog viewDeploymentDialog = new ViewDeploymentDialog();
            viewDeploymentDialog.ShowModal();
        }
    }
}
