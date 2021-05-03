using Microsoft.VisualStudio.Shell;
using PortingAssistantVSExtensionClient.Common;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;

namespace PortingAssistantVSExtensionClient.Options
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    [Guid(GeneralOption.GeneralOptionGuidString)]
    public class GeneralOption : UIElementDialogPage
    {
        public const string GeneralOptionGuidString = "459594a1-6b43-4e64-a335-13b1b5581836";

        private readonly OptionPageControl _optionsPageControl;

        private readonly UserSettings _userSettings;

        public GeneralOption()
        {
            _optionsPageControl = new OptionPageControl();
            _userSettings = UserSettings.Instance;
            foreach (string framwork in TargetFrameworkType.ALL_SElECTION)
            {
                _optionsPageControl.TargeFrameworks.Items.Add(framwork);
            }
        }

        protected override UIElement Child { get { return _optionsPageControl; } }

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
            _optionsPageControl.TargeFrameworks.SelectedItem = _userSettings.TargetFramework; 
        }
        
        void Save()
        {
            _userSettings.TargetFramework = (string)_optionsPageControl.TargeFrameworks.SelectedValue;
            _userSettings.UpdateTargetFramework();
        }
    }
}
