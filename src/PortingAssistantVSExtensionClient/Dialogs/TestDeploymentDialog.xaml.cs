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
using System.Reflection;

namespace PortingAssistantVSExtensionClient.Dialogs
{
    /// <summary>
    /// Interaction logic for TestDeploymentDialog.xaml
    /// </summary>
    public partial class TestDeploymentDialog : DialogWindow
    {
        private const string SELECTED_BACKGROUND_COLOR = "#FFE5E5E5";
        public DeploymentParameters parameters { set; get; }
        private readonly UserSettings _userSettings;
        private readonly string SolutionPath;
        private Dictionary<string, string> directories = new Dictionary<string, string>();
        private readonly Dictionary<string, string> projectNames;
        private readonly string JsonConfigurationPath;

        public TestDeploymentDialog(string solutionPath)
        {
            InitializeComponent();
            _userSettings = UserSettings.Instance;
            this.SolutionPath = solutionPath;
            this.Title = "Test Deployment";
            this.JsonConfigurationPath = "\"" + Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                Common.Constants.ResourceFolder,
                "deploymentconfiguration.json") + "\"";
            DeploymentNameTextBox.Text = GetSolutionName(SolutionPath);
            var projectFiles = SolutionUtils.GetProjectPath(SolutionPath);
            projectNames = projectFiles.Where(p => p.Length != 0).ToDictionary(p => GetProjectName(p), p => p);
            //init profile
            List<string> profiles = AwsUtils.ListProfiles();
            foreach (string profile in profiles)
            {
                AwsProfileComboBox.Items.Add(profile);
            }
            if (profiles != null && profiles.Count != 0)
            {
                AwsProfileComboBox.SelectedValue = _userSettings.AWSProfileName;
            }

            //init project
            foreach (string projectName in projectNames.Keys)
            {
                DeploymentProjectComboBox.Items.Add(projectName);
            }

            foreach (var region in RegionEndpoint.EnumerableAllRegions)
            {
                AwsRegionComboBox.Items.Add(region.SystemName);
            }
        }


        private string GetProjectName(string projectPath)
        {
            return Path.GetFileNameWithoutExtension(projectPath);
        }

        private string GetSolutionName(string solutionPath)
        {
            return Path.GetFileNameWithoutExtension(solutionPath);
        }
        public static DeploymentParameters GetParameters(string solutionPath)
        {
            TestDeploymentDialog testDeploymenttDialog = new TestDeploymentDialog(solutionPath);
            testDeploymenttDialog.ShowModal();
            return testDeploymenttDialog.parameters;
        }

        private void Next_Button_Click_1(object sender, System.Windows.RoutedEventArgs e)
        {
            if (IsInputValid())
            {
                HideGeneralSettings(true);
                HideVPCSettings(false);
                HideAdvanceSettings(true);
                HideReviewSettings(true);

                string profleNameSelected = (string)AwsProfileComboBox.SelectedValue;
                List<string> vpcs = AwsUtils.ListVpcIds(profleNameSelected, RegionEndpoint.GetBySystemName(AwsRegionComboBox.Text));
                foreach (var vpc in vpcs)
                {
                    VpcBox.Items.Clear();
                    VpcBox.Items.Add(vpc);
                }
            }
        }

        private void Next_Button_Click_2(object sender, System.Windows.RoutedEventArgs e)
        {
            if (IsInputValid())
            {
                HideGeneralSettings(true);
                HideVPCSettings(true);
                HideAdvanceSettings(false);
                HideReviewSettings(true);

                Review_Application_Name_Textblock.Text = DeploymentNameTextBox.Text;
                Review_Prfile_Textblock.Text = (string)AwsProfileComboBox.SelectedValue;
                Review_Project_Textblock.Text = (string)DeploymentProjectComboBox.SelectedValue;
                Review_Region_Textblock.Text = (string)AwsRegionComboBox.SelectedValue;
                Review_VPC_Textblock.Text = NewVPCCheck.IsChecked.HasValue && NewVPCCheck.IsChecked.Value ? "Create New VPC" : VpcBox.Text;
            }

        }

        private void Next_Button_Click_3(object sender, System.Windows.RoutedEventArgs e)
        {
            HideGeneralSettings(true);
            HideVPCSettings(true);
            HideAdvanceSettings(true);
            HideReviewSettings(false);

        }
        private void Back_Click_2(object sender, System.Windows.RoutedEventArgs e)
        {
            HideGeneralSettings(false);
            HideVPCSettings(true);
            HideAdvanceSettings(true);
            HideReviewSettings(true);
        }

        private void Back_Button_Click_3(object sender, System.Windows.RoutedEventArgs e)
        {
            HideGeneralSettings(true);
            HideVPCSettings(false);
            HideAdvanceSettings(true);
            HideReviewSettings(true);
        }

        private void Back_Button_Click_4(object sender, System.Windows.RoutedEventArgs e)
        {
            HideGeneralSettings(true);
            HideVPCSettings(true);
            HideAdvanceSettings(false);
            HideReviewSettings(true);
        }

        private void Deploy_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //validations
            if (IsInputValid())
            {
                var selectedProfileName = (string)AwsProfileComboBox.SelectedValue;
                var selectedProject = (string)DeploymentProjectComboBox.SelectedValue;
                var initTool = selectedProfileName != _userSettings.AWSProfileName;
                parameters = new DeploymentParameters()
                {
                    profileName = selectedProfileName,
                    enableMetrics = _userSettings.EnabledMetrics,
                    deployname = DeploymentNameTextBox.Text,
                    selectedProject = projectNames[selectedProject],
                    vpcId = NewVPCCheck.IsChecked.HasValue && NewVPCCheck.IsChecked.Value ? "" : VpcBox.Text,
                    initDeploymentTool = initTool
                };

                if (initTool) _userSettings.UpdateDeploymentProfileName(selectedProfileName);
                Close();
            }
        }

        private void AdvancedSettingsFrame_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (AdvancedSettingsGrid.Visibility == System.Windows.Visibility.Visible || !IsInputValid()) return;
            HideGeneralSettings(true);
            HideVPCSettings(true);
            HideAdvanceSettings(false);
            HideReviewSettings(true);
        }

        private void GeneralSettingsFrame_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (GeneralSettingGrid.Visibility == System.Windows.Visibility.Visible || !IsInputValid()) return;
            HideGeneralSettings(false);
            HideVPCSettings(true);
            HideAdvanceSettings(true);
            HideReviewSettings(true);
        }

        private void Review_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ReviewGrid.Visibility == System.Windows.Visibility.Visible || !IsInputValid()) return;
            HideGeneralSettings(true);
            HideVPCSettings(true);
            HideAdvanceSettings(true);
            HideReviewSettings(false);
        }

        private void VPC_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (VPCGrid.Visibility == System.Windows.Visibility.Visible || !IsInputValid()) return;
            HideGeneralSettings(true);
            HideVPCSettings(false);
            HideAdvanceSettings(true);
            HideReviewSettings(true);
        }

        private void HideGeneralSettings(bool hide)
        {
            if (hide)
            {
                GeneralSettingGrid.Visibility = System.Windows.Visibility.Hidden;
                GeneralSettingsFrame.Fill = null;
            }
            else
            {
                GeneralSettingGrid.Visibility = System.Windows.Visibility.Visible;
                GeneralSettingsFrame.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(SELECTED_BACKGROUND_COLOR));
            }
        }

        private void HideVPCSettings(bool hide)
        {
            if (hide)
            {
                VPCGrid.Visibility = System.Windows.Visibility.Hidden;
                VPCFrame.Fill = null;
            }
            else
            {
                VPCGrid.Visibility = System.Windows.Visibility.Visible;
                VPCFrame.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(SELECTED_BACKGROUND_COLOR));
            }
        }

        private void HideAdvanceSettings(bool hide)
        {
            if (hide)
            {
                AdvancedSettingsGrid.Visibility = System.Windows.Visibility.Hidden;
                AdvancedSettingsFrame.Fill = null;
            }
            else
            {
                AdvancedSettingsGrid.Visibility = System.Windows.Visibility.Visible;
                AdvancedSettingsFrame.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(SELECTED_BACKGROUND_COLOR));
            }
        }

        private void HideReviewSettings(bool hide)
        {
            if (hide)
            {
                ReviewGrid.Visibility = System.Windows.Visibility.Hidden;
                ReviewFrame.Fill = null;

            }
            else
            {
                ReviewGrid.Visibility = System.Windows.Visibility.Visible;
                ReviewFrame.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(SELECTED_BACKGROUND_COLOR));
            }
        }



        private void VpcBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            VpcSubnetsBox.IsEnabled = false;
            string profleNameSelected = (string)AwsProfileComboBox.SelectedValue;
            LoadSubnets((string)VpcBox.SelectedValue, profleNameSelected, RegionEndpoint.GetBySystemName(AwsRegionComboBox.Text));
            VpcSubnetsBox.IsEnabled = true;
        }

        private void LoadSubnets(string vpcId, string profileName, RegionEndpoint region)
        {
            VpcSubnetsBox.Items.Clear();
            var subnets = AwsUtils.ListVpcSubnets(vpcId, profileName, region);
            foreach (var subnet in subnets)
            {
                VpcSubnetsBox.Items.Add(subnet);
            }
        }


        private bool IsInputValid()
        {
            bool IsValid = true;
            if (GeneralSettingGrid.Visibility == System.Windows.Visibility.Visible)
            {
                if (AwsProfileComboBox.SelectedIndex == -1)
                {
                    ProfileErrorHint.Content = "please select an AWS profile";
                    IsValid = false;
                }
                else
                {
                    ProfileErrorHint.Content = "";
                }
                if (AwsRegionComboBox.SelectedIndex == -1)
                {
                    RegioneErrorHint.Content = "please select an AWS Region";
                    IsValid = false;
                }
                else
                {
                    RegioneErrorHint.Content = "";
                }
                if (DeploymentProjectComboBox.SelectedIndex == -1)
                {
                    ProjectErrorHint.Content = "please select an project to deploy";
                    IsValid = false;
                }
                else
                {
                    ProjectErrorHint.Content = "";
                }
                if (string.IsNullOrEmpty(DeploymentNameTextBox.Text))
                {
                    DeploymentNameErrorHint.Content = "please enter deployment name";
                    IsValid = false;
                }
                else
                {
                    DeploymentNameErrorHint.Content = "";
                }
            }
            else if (VPCGrid.Visibility == System.Windows.Visibility.Visible)
            {
                if (ExistingVPCCheck.IsChecked.HasValue && ExistingVPCCheck.IsChecked.Value && VpcBox.SelectedIndex == -1)
                {
                    IsValid = false;
                    VpcErrorHint.Content = "please select a vpc";
                }
                else
                {
                    VpcErrorHint.Content = "";
                }
                if (ExistingVPCCheck.IsChecked.HasValue && ExistingVPCCheck.IsChecked.Value && VpcSubnetsBox.SelectedIndex == -1)
                {
                    IsValid = false;
                    VpcSubnetErrorHint.Content = "please select a vpc subnet";
                }
                else
                {
                    VpcSubnetErrorHint.Content = "";
                }
            }
            return IsValid;
        }
        private void Cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void RadioButton_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            ExistingVPCGrid.Visibility = System.Windows.Visibility.Visible;
        }

        private void RadioButton_Checked_1(object sender, System.Windows.RoutedEventArgs e)
        {
            ExistingVPCGrid.Visibility = System.Windows.Visibility.Hidden;
        }

        private void JsonFileUrl_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Process.Start(JsonConfigurationPath);
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
