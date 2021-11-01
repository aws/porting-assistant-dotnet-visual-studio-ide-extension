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
            DataTable dataTable = new DataTable();
            DataColumn[] columns = { new DataColumn("ID"), new DataColumn("Value") };
            Object[] row1 = { "1", "Value1" };
            Object[] row2 = { "2", "Value2" };
            Object[] row3 = { "3", "Value3" };
            dataTable.Columns.AddRange(columns);
            dataTable.Rows.Add(row1);
            dataTable.Rows.Add(row2);
            dataTable.Rows.Add(row3);
        }

        public static void Execute()
        {
            ViewDeploymentDialog viewDeploymentDialog = new ViewDeploymentDialog();
            viewDeploymentDialog.ShowModal();
        }
    }
}
