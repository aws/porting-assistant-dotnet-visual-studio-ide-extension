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
    [Guid("459594a1-6b43-4e64-a335-13b1b5581836")]
    public class GeneralOption : UIElementDialogPage
    {
        private readonly OptionPageControl _optionsPageControl;

        private readonly UserSettings _userSettings;

        public GeneralOption()
        {
            _optionsPageControl = new OptionPageControl();
            _userSettings = UserSettings.Instance;
            foreach (TargetFrameworkType framwork in Enum.GetValues(typeof(TargetFrameworkType)))
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
            _userSettings.TargetFramework = (TargetFrameworkType)_optionsPageControl.TargeFrameworks.SelectedValue;
            _userSettings.SaveAllSettings();
        }
    }
}
