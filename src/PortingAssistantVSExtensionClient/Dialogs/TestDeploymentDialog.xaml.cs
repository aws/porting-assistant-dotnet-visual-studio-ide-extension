﻿using Microsoft.VisualStudio.PlatformUI;
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

        private static TestDeploymentDialog Instance;
        public TestDeploymentDialog(string solutionPath)
        {
            InitializeComponent();
            _userSettings = UserSettings.Instance;
            this.SolutionPath = solutionPath;
            this.Title = "Test Deployment";
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
                HideDirectorySettings(false);
                HideAdvanceSettings(true);
            }
        }

        private void Next_Button_Click_2(object sender, System.Windows.RoutedEventArgs e)
        {
            if (IsInputValid())
            {
                HideGeneralSettings(true);
                HideDirectorySettings(true);
                HideAdvanceSettings(false);
            }

        }

        private void Back_Click_2(object sender, System.Windows.RoutedEventArgs e)
        {
            HideGeneralSettings(false);
            HideDirectorySettings(true);
            HideAdvanceSettings(true);
        }

        private void Next_Button_Click_3(object sender, System.Windows.RoutedEventArgs e)
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
                    directoryId = ADIdBox.Text,
                    domainSecretsArn = (string)SecretArnBox.SelectedValue,
                    servicePrincipalName = SecretPrincipalBox.Text,
                    vpcId = VpcBox.Text,
                    initDeploymentTool = initTool
                };

                if (initTool) _userSettings.UpdateDeploymentProfileName(selectedProfileName);
                Close();
            }
        }

        private void Back_Button_Click_3(object sender, System.Windows.RoutedEventArgs e)
        {
            HideGeneralSettings(true);
            HideDirectorySettings(false);
            HideAdvanceSettings(true);
        }


        private void AdvancedSettingsFrame_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (AdvancedSettingsGrid.Visibility == System.Windows.Visibility.Visible || !IsInputValid()) return;
            HideGeneralSettings(true);
            HideDirectorySettings(true);
            HideAdvanceSettings(false);
        }

        private void DirectoryServiceFrame_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DirectoryServicesGrid.Visibility == System.Windows.Visibility.Visible || !IsInputValid()) return;
            HideGeneralSettings(true);
            HideDirectorySettings(false);
            HideAdvanceSettings(true);
        }

        private void GeneralSettingsFrame_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (GeneralSettingGrid.Visibility == System.Windows.Visibility.Visible || !IsInputValid()) return;
            HideGeneralSettings(false);
            HideDirectorySettings(true);
            HideAdvanceSettings(true);
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

        private void HideDirectorySettings(bool hide)
        {
            if (hide)
            {
                DirectoryServicesGrid.Visibility = System.Windows.Visibility.Hidden;
                DirectoryServiceFrame.Fill = null;
            }
            else
            {
                DirectoryServicesGrid.Visibility = System.Windows.Visibility.Visible;
                DirectoryServiceFrame.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(SELECTED_BACKGROUND_COLOR));
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

        private void CheckBox_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            ADGrid.Visibility = System.Windows.Visibility.Visible;
        }

        private void CheckBox_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            ADGrid.Visibility = System.Windows.Visibility.Hidden;
        }

        private void VpcBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            VpcSubnetsBox.IsEnabled = false;
            string profleNameSelected = (string)AwsProfileComboBox.SelectedValue;
            LoadSubnets((string)VpcBox.SelectedValue, profleNameSelected);
            VpcSubnetsBox.IsEnabled = true;
        }

        private void LoadSubnets(string vpcId, string profileName)
        {
            VpcSubnetsBox.Items.Clear();
            var subnets = AwsUtils.ListVpcSubnets(vpcId, profileName);
            foreach (var subnet in subnets)
            {
                VpcSubnetsBox.Items.Add(subnet);
            }
        }

        private void ADNameBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (directories.TryGetValue((string)ADNameBox.SelectedValue, out var directoryId))
            {
                ADIdBox.Text = directoryId;
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
                if (AwsRegionComboBox.SelectedIndex == -1)
                {
                    RegioneErrorHint.Content = "please select an AWS Region";
                    IsValid = false;
                }
                if (DeploymentProjectComboBox.SelectedIndex == -1)
                {

                    IsValid = false;
                }
                if (string.IsNullOrEmpty(DeploymentNameTextBox.Text))
                {

                    IsValid = false;
                }
            }
            else if (DirectoryServicesGrid.Visibility == System.Windows.Visibility.Visible)
            {
                if (UseAwsAdCheckBox.IsChecked.HasValue && UseAwsAdCheckBox.IsChecked.Value)
                {
                    if (string.IsNullOrEmpty(ADIdBox.Text))
                    {
                        IsValid = false;
                    }
                    if (SecretArnBox.SelectedIndex == -1)
                    {
                        IsValid = false;
                    }
                    if (string.IsNullOrEmpty(SecretPrincipalBox.Text))
                    {
                        IsValid = false;
                    }
                }
            }
            else if (AdvancedSettingsGrid.Visibility == System.Windows.Visibility.Visible)
            {
                if (VpcBox.SelectedIndex == -1)
                {
                    IsValid = false;
                }
                if (VpcSubnetsBox.SelectedIndex == -1)
                {
                    IsValid = false;
                }
            }
            return IsValid;
        }

        private void AwsProfileComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            string profleNameSelected = (string)AwsProfileComboBox.SelectedValue;

            foreach (var region in RegionEndpoint.EnumerableAllRegions)
            {
                AwsRegionComboBox.Items.Add(region.DisplayName);
            }

            directories = AwsUtils.ListActiveDirectories(profleNameSelected);
            foreach (var directoryName in directories.Keys)
            {
                ADNameBox.Items.Add(directoryName);
            }

            List<string> arns = AwsUtils.ListSecretArns(profleNameSelected);
            foreach (var arn in arns)
            {
                SecretArnBox.Items.Add(arn);
            }

            List<string> vpcs = AwsUtils.ListVpcIds(profleNameSelected);
            foreach (var vpc in vpcs)
            {
                VpcBox.Items.Add(vpc);
            }
        }
    }
}
