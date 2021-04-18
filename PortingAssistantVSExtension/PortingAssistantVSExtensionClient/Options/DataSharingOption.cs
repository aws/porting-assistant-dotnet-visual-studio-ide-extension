using Microsoft.VisualStudio.Shell;
using PortingAssistantVSExtensionClient.Models;
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

        public DataSharingOption()
        {
            _userSettings = UserSettings.Instance;
            _dataSharingoptionsPageControl = new DataSharingOptionPage();
            _dataSharingoptionsPageControl.InitalizeNamedProfile(_userSettings.AWSProfileName);
        }

        protected override UIElement Child { get { return _dataSharingoptionsPageControl; } }

        protected override void OnActivate(CancelEventArgs e)
        {
            base.OnActivate(e);
            LoadSettings();
        }
        protected override void OnApply(PageApplyEventArgs args)
        {
            Save();
        }

        void LoadSettings()
        {
            _dataSharingoptionsPageControl.EnableMetricCheck.IsChecked = _userSettings.EnabledMetrics;
            if (_dataSharingoptionsPageControl.Profiles.Items.Contains(_userSettings.AWSProfileName))
            {
                _dataSharingoptionsPageControl.Profiles.SelectedItem = _userSettings.AWSProfileName;
            }
            else
            {
                _dataSharingoptionsPageControl.Profiles.SelectedItem = "";
            }
            
        }

        void Save()
        {
            _userSettings.AWSProfileName = (string)_dataSharingoptionsPageControl.Profiles.SelectedValue;
            _userSettings.EnabledMetrics = _dataSharingoptionsPageControl.EnableMetricCheck.IsChecked ?? false;
            _userSettings.SaveAllSettings();
            PortingAssistantLanguageClient.UpdateUserSettingsAsync();
        }
    }
}
