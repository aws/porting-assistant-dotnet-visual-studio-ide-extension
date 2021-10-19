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

namespace PortingAssistantVSExtensionClient.Dialogs
{
    /// <summary>
    /// Interaction logic for TestDeploymentDialog.xaml
    /// </summary>
    public partial class TestDeploymentDialog : DialogWindow
    {
        private const string SELECTED_BACKGROUND_COLOR = "#FFE5E5E5";
        private const string DEFAULT_DEPLOYMENT_NAME = ".Net Test deploy";
        public DeploymentParameters parameters { set; get; }
        private readonly UserSettings _userSettings;

        private static TestDeploymentDialog Instance;
        public TestDeploymentDialog()
        {
            InitializeComponent();
            _userSettings = UserSettings.Instance;
            parameters = new DeploymentParameters();
            this.Title = "Test Deployment";
            DeploymentName.Text = DEFAULT_DEPLOYMENT_NAME;
        }

        public static DeploymentParameters GetParameters()
        {
            TestDeploymentDialog testDeploymenttDialog = GetInstance();
            testDeploymenttDialog.ShowModal();
            return testDeploymenttDialog.parameters;
        }

        private static TestDeploymentDialog GetInstance()
        {
            if (Instance == null)
            {
                Instance = new TestDeploymentDialog();
            }
            return Instance;
        }

        private void Browse_Folder_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            BrowseFolder();
        }

        private void Next_Button_Click_1(object sender, System.Windows.RoutedEventArgs e)
        {
            HideGeneralSettings(true);
            HideDirectorySettings(false);
            HideAdvanceSettings(true);
            HideEULA(true);
            
        }

        private void Next_Button_Click_2(object sender, System.Windows.RoutedEventArgs e)
        {
            HideGeneralSettings(true);
            HideDirectorySettings(true);
            HideAdvanceSettings(false);
            HideEULA(true);
        }

        private void Back_Click_2(object sender, System.Windows.RoutedEventArgs e)
        {
            HideGeneralSettings(false);
            HideDirectorySettings(true);
            HideAdvanceSettings(true);
            HideEULA(true);
        }

        private void Next_Button_Click_3(object sender, System.Windows.RoutedEventArgs e)
        {
            HideGeneralSettings(true);
            HideDirectorySettings(true);
            HideAdvanceSettings(true);
            HideEULA(false);
        }

        private void Back_Button_Click_3(object sender, System.Windows.RoutedEventArgs e)
        {
            HideGeneralSettings(true);
            HideDirectorySettings(false);
            HideAdvanceSettings(true);
            HideEULA(true);
        }

        private void Back_Button_Click_4(object sender, System.Windows.RoutedEventArgs e)
        {
            HideGeneralSettings(true);
            HideDirectorySettings(true);
            HideAdvanceSettings(false);
            HideEULA(true);
        }

        private void LicenseAgreementFrame_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (LicenseAgreementGrid.Visibility == System.Windows.Visibility.Visible) return;
            HideGeneralSettings(true);
            HideDirectorySettings(true);
            HideAdvanceSettings(true);
            HideEULA(false);
        }

        private void AdvancedSettingsFrame_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (AdvancedSettingsGrid.Visibility == System.Windows.Visibility.Visible) return;
            HideGeneralSettings(true);
            HideDirectorySettings(true);
            HideAdvanceSettings(false);
            HideEULA(true);
        }

        private void DirectoryServiceFrame_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DirectoryServicesGrid.Visibility == System.Windows.Visibility.Visible) return;
            HideGeneralSettings(true);
            HideDirectorySettings(false);
            HideAdvanceSettings(true);
            HideEULA(true);
        }

        private void GeneralSettingsFrame_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (GeneralSettingGrid.Visibility == System.Windows.Visibility.Visible) return;
            HideGeneralSettings(false);
            HideDirectorySettings(true);
            HideAdvanceSettings(true);
            HideEULA(true);
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

        private void HideEULA(bool hide)
        {
            if (hide)
            {
                LicenseAgreementGrid.Visibility = System.Windows.Visibility.Hidden;
                LicenseAgreementFrame.Fill = null;
            }
            else
            {
                LicenseAgreementGrid.Visibility = System.Windows.Visibility.Visible;
                LicenseAgreementFrame.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(SELECTED_BACKGROUND_COLOR));
            }
        }
        private void BrowseFolder()
        {
            FolderBrowserDialog openFolderDialog = new FolderBrowserDialog();

            if (openFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                LocalFolderPathText.Text = openFolderDialog.SelectedPath;
            }
            parameters.buildFolderPath = LocalFolderPathText.Text;

            var oldProfileName = _userSettings.GetDeploymentProfileName();
            parameters.initDeploymentTool = oldProfileName == "current_profile" ? false : true;
            parameters.enableMetrics = _userSettings.EnabledMetrics;
        }

        private void CheckBox_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            ADGrid.Visibility = System.Windows.Visibility.Visible;
        }

        private void CheckBox_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            ADGrid.Visibility = System.Windows.Visibility.Hidden;
        }
    }
}
