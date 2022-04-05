using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;

namespace PortingAssistantVSExtensionClient.Options
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    [Guid("459594a1-6b43-4e64-a335-13b1b5581837")]
    class DataSharingOption: UIElementDialogPage
    {
        private readonly DataSharingOptionPage _dataSharingoptionsPageControl;

        private readonly UserSettings _userSettings;

        private bool awsProfileFlag = true;

        public DataSharingOption()
        {
            _userSettings = UserSettings.Instance;
            _dataSharingoptionsPageControl = new DataSharingOptionPage();
            
        }

        protected override UIElement Child { get { return _dataSharingoptionsPageControl; } }

        protected override void OnActivate(CancelEventArgs e)
        {
            base.OnActivate(e);
            LoadSettings();
            if (_dataSharingoptionsPageControl.Profiles.SelectedItem == null
                || _dataSharingoptionsPageControl.Profiles.SelectedItem.Equals(""))
            {
                _dataSharingoptionsPageControl.WarningBar.Content = "Profile is required";
            }
        }
        protected override void OnApply(PageApplyEventArgs args)
        {
            Save();
        }

        void LoadSettings()
        {
            _dataSharingoptionsPageControl.EnableMetricCheck.IsChecked = _userSettings.EnabledMetrics;
            _dataSharingoptionsPageControl.InitalizeNamedProfile(_userSettings.AWSProfileName);
        }

        void Save()
        {
            if (_dataSharingoptionsPageControl.AWSProfileSelect.IsChecked != null &&
                (bool)_dataSharingoptionsPageControl.AWSProfileSelect.IsChecked)
            {
                _userSettings.AWSProfileName = (string)_dataSharingoptionsPageControl.Profiles.SelectedValue;
                _userSettings.EnabledDefaultCredentials = false;
            }
            else
            {
                _userSettings.AWSProfileName = "DEFAULT_PROFILE";
                _userSettings.EnabledDefaultCredentials = true;
            }

            _userSettings.EnabledMetrics = _dataSharingoptionsPageControl.EnableMetricCheck.IsChecked ?? false;
            _userSettings.SaveAllSettings();
            PortingAssistantLanguageClient.UpdateUserSettingsAsync();
        }
    }
}
