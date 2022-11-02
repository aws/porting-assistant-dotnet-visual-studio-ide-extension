using Microsoft.VisualStudio.Shell;
using PortingAssistantExtensionClientShared.Models;
using PortingAssistantVSExtensionClient.Common;
using System;
using System.Collections.Generic;
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
            _optionsPageControl.TargeFrameworks.Items.Clear();
            // Sort based on recommended order.
            if (PortingAssistantLanguageClient.Instance.SupportedVersionConfiguration?.Versions != null)
            {
                foreach (var version in PortingAssistantLanguageClient.Instance.SupportedVersionConfiguration.Versions)
                {
                    if (Version.TryParse(version.RequiredVisualStudioVersion, out Version requiredVSVersion) &&
                        requiredVSVersion <= PortingAssistantLanguageClient.Instance.VisualStudioVersion)
                    {
                        _optionsPageControl.TargeFrameworks.Items.Add(version.DisplayName);
                    }
                }
            }

            _optionsPageControl.TargeFrameworks.SelectedItem = 
                PortingAssistantLanguageClient
                    .Instance
                    .SupportedVersionConfiguration?
                    .GetDisplayName(_userSettings.TargetFramework);
        }

        void Save()
        {
            _userSettings.TargetFramework =
                PortingAssistantLanguageClient
                    .Instance
                    .SupportedVersionConfiguration?
                    .GetVersionKey((string)_optionsPageControl.TargeFrameworks.SelectedItem);

            _userSettings.UpdateTargetFramework();
        }
    }
}
