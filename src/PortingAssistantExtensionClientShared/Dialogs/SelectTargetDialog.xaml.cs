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
            foreach (string framwork in TargetFrameworkType.ALL_SElECTION)
            {
                TargetFrameWorkDropDown.Items.Add(framwork);
            }
#if Dev16
            TargetFrameWorkDropDown.Items.Remove(TargetFrameworkType.NET60);
#endif
            TargetFrameWorkDropDown.SelectedValue = TargetFrameworkType.NO_SELECTION;
            this.Title = "Choose a Target Framework";
        }

        public static bool EnsureExecute()
        {
            SelectTargetDialog selectTargetDialog = new SelectTargetDialog();
            selectTargetDialog.ShowModal();
            return selectTargetDialog.ClickResult;
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
