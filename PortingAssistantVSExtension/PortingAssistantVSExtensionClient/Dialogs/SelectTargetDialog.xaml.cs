using Microsoft.VisualStudio.PlatformUI;
using PortingAssistantVSExtensionClient.Models;
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
            foreach (TargetFrameworkType framwork in Enum.GetValues(typeof(TargetFrameworkType)))
            {
                TargetFrameWorkDropDown.Items.Add(framwork);
            }
            TargetFrameWorkDropDown.SelectedValue = TargetFrameworkType.no_selection;
        }

        public static bool EnsureExecute()
        {
            SelectTargetDialog selectTargetDialog = new SelectTargetDialog();
            selectTargetDialog.ShowModal();
            return selectTargetDialog.ClickResult;
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (TargetFrameWorkDropDown.SelectedValue.Equals(TargetFrameworkType.no_selection))
            {
                ChooseFrameworkLabel.Content = "Please make a selection of target framework!";
            }
            else
            {
                _userSettings.TargetFramework = (TargetFrameworkType)TargetFrameWorkDropDown.SelectedValue;
                _userSettings.SaveAllSettings();
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
