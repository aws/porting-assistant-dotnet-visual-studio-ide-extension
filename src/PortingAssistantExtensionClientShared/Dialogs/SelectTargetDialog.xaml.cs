using Microsoft.VisualStudio.PlatformUI;
using PortingAssistantVSExtensionClient.Common;
using PortingAssistantVSExtensionClient.Options;
using System;

namespace PortingAssistantVSExtensionClient.Dialogs
{
    public partial class SelectTargetDialog : DialogWindow
    {
        private readonly UserSettings _userSettings;
        public bool ClickResult = false;
        public SelectTargetDialog()
        {
            _userSettings = UserSettings.Instance;
            InitializeComponent();
            this.Title = "Choose a Target Framework";
            PrepareSupportedVersions();
        }

        public static bool EnsureExecute()
        {
            SelectTargetDialog selectTargetDialog = new SelectTargetDialog();
            selectTargetDialog.ShowModal();
            return selectTargetDialog.ClickResult;
        }

        private void PrepareSupportedVersions()
        {
            TargetFrameWorkDropDown.Items.Clear();
            // Sort based on recommended order.
            if (PortingAssistantLanguageClient.Instance.SupportedVersionConfiguration?.Versions != null)
            {
                foreach (var version in PortingAssistantLanguageClient.Instance.SupportedVersionConfiguration.Versions)
                {
                    if (Version.TryParse(version.RequiredVisualStudioVersion, out Version requiredVSVersion) &&
                        requiredVSVersion <= PortingAssistantLanguageClient.Instance.VisualStudioVersion)
                    {
                        TargetFrameWorkDropDown.Items.Add(version.DisplayName);
                    }
                }
            }

            TargetFrameWorkDropDown.SelectedItem = TargetFrameworkType.NO_SELECTION;
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (TargetFrameWorkDropDown.SelectedValue.Equals(TargetFrameworkType.NO_SELECTION))
            {
                ChooseFrameworkLabel.Content = "Please make a selection of target framework!";
            }
            else
            {
                _userSettings.TargetFramework = (string)TargetFrameWorkDropDown.SelectedValue;
                _userSettings.UpdateTargetFramework();
                ClickResult = true;
                Close();
            }
        }

        private void Button_Click_1(object sender, System.Windows.RoutedEventArgs e)
        {
            ClickResult = false;
            Close();
        }
    }
}
