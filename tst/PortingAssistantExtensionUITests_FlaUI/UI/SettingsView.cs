using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace IDE_UITest.UI
{
    public class SettingsView : ElementBase
    {
        public SettingsView(FrameworkAutomationElementBase frameworkAutomationElement) : base(frameworkAutomationElement)
        {
        }

        internal Window Pane => WaitForElement(() => FindFirstChild(e => e.ByControlType(FlaUI.Core.Definitions.ControlType.Pane))).AsWindow();
        internal Window TargetFrameworkGroupBox => WaitForElement(() => 
            Pane.FindFirstDescendant(e => e.ByName("Target Framework").And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Group)))).AsWindow();
        internal ComboBox TargetFrameworkComboBox => WaitForElement(() => 
            TargetFrameworkGroupBox.FindFirstChild(e => e.ByAutomationId("TargeFrameworks").And(e.ByControlType(FlaUI.Core.Definitions.ControlType.ComboBox)))).AsComboBox();
        internal Button ClearCacheBtn => WaitForElement(() => TargetFrameworkGroupBox.FindFirstChild(e => e.ByAutomationId("ClearCache"))).AsButton();
        internal Button OkBtn => WaitForElement(() => FindFirstChild(e => e.ByName("OK"))).AsButton();
        internal Button CancelBtn => WaitForElement(() => FindFirstChild(e => e.ByName("Cancel"))).AsButton();

        internal Tree CategoriesTree => WaitForElement(() => FindFirstChild(e => e.ByName("Categories:"))).AsTree();
        internal TreeItem DataUsageSharingTreeItem => WaitForElement(() => CategoriesTree.FindFirstDescendant(e => e.ByName("Data usage sharing"))).AsTreeItem();


        internal AutomationElement PAGroup => WaitForElement(() => Pane.FindFirstDescendant(e => e.ByName("Porting Assistant for .NET").
        And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Group))));
        internal AutomationElement DataUsageSharingGroup => WaitForElement(() => Pane.FindFirstDescendant(e => e.ByName("Porting Assistant for .NET data usage sharing").
        And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Group))));

        internal CheckBox EnableMatricCheckbox => WaitForElement(() => PAGroup.FindFirstChild(e => e.ByAutomationId("EnableMetricCheck").
         And(e.ByControlType(FlaUI.Core.Definitions.ControlType.CheckBox))).AsCheckBox());

        internal ComboBox ProfilesComboBox => WaitForElement(() =>
            PAGroup.FindFirstChild(e => e.ByAutomationId("Profiles").And(e.ByControlType(FlaUI.Core.Definitions.ControlType.ComboBox)))).AsComboBox();

        internal Button AddProfileBtn => WaitForElement(() => PAGroup.FindFirstChild(e => e.ByName("Add a Named Profile"))).AsButton();

        internal AutomationElement ToLearnMoreHyperLink => WaitForElement(() => DataUsageSharingGroup.FindFirstDescendant(e => e.ByClassName("Hyperlink").
          And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Hyperlink))));

        internal RadioButton UseAWSProfileRadioButton => WaitForElement(() => PAGroup.FindFirstChild(e => e.ByAutomationId("AWSProfileSelect").
            And(e.ByControlType(FlaUI.Core.Definitions.ControlType.RadioButton))).AsRadioButton());
        internal RadioButton DefaultProviderChainRadioButton => WaitForElement(() => PAGroup.FindFirstChild(e => e.ByAutomationId("SDKChainSelect").
            And(e.ByControlType(FlaUI.Core.Definitions.ControlType.RadioButton))).AsRadioButton());

        public void SelectTargetFrameWork(string targetfw)
        {
            AutomationElement[] listItems = GetTargetFrameworkOptions();

            var l = listItems.FirstOrDefault(l => l.Name == targetfw).AsListBoxItem();
            Assert.True(l != null, $"Fail to find target framework [{targetfw}]");
            l.DrawHighlight();
            TargetFrameworkComboBox.Select(targetfw);
        }

        public void ClearCache()
        {
            ClearCacheBtn.WaitUntilClickable();
            ClearCacheBtn.Invoke();
            Assert.False(ClearCacheBtn.IsEnabled, $"{ClearCacheBtn.Name} should be disabled after clicking");
        }

        private AutomationElement[] GetTargetFrameworkOptions()
        {
            TargetFrameworkComboBox.DrawHighlight();
            TargetFrameworkComboBox.Expand();
            Retry.WhileFalse(() =>
            {
                var listItems = FindAllDescendants(e => e.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem));
                return listItems?.Length > 0;
            });
            var listItems = FindAllDescendants(e => e.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem));
            return listItems;
        }

        public void VerifyExpectedTargetFrameworkOption(Constants.Version vsVersion)
        {
            var tfOptions = GetTargetFrameworkOptions().Select(i => i.Name).ToList();
            var expectedOptions2019 = new List<string>() { "netcoreapp3.1", "net5.0" };
            var expectedOptions2022 = new List<string>() { ".NET Core 3.1 (Microsoft LTS)", ".NET 5 (Microsoft out of support)", ".NET 6 (Microsoft LTS)", ".NET 7 (Standard Term Support)" };
            switch (vsVersion) {
                case Constants.Version.VS2019:
                    Assert.True(expectedOptions2019.All(tfOptions.Contains), 
                        "Expect target framework option for visualstudio 2019 is netcoreapp3.1 and net5");
                    break;
                case Constants.Version.VS2022:
                    Assert.True(expectedOptions2022.All(tfOptions.Contains),
                        "Expect target framework option for visualstudio 2022 is netcoreapp3.1, net5, net6 and net7");
                    break;
            }
        }

        public void SaveSettings()
        {
            OkBtn.WaitUntilClickable();
            OkBtn.DrawHighlight();
            OkBtn.Invoke();
        }

        public void SelectDefaultSDKChainRadioButton()
        {
            DefaultProviderChainRadioButton.WaitUntilClickable();
            DefaultProviderChainRadioButton.DrawHighlight();
            DefaultProviderChainRadioButton.Click();
        }

        public void SelectAWSProfileRadioButton()
        {
            UseAWSProfileRadioButton.DrawHighlight();
            UseAWSProfileRadioButton.Click();
        }

        public void SelectAwsProfileByName(string name)
        {
            List<AutomationElement> listItems = GetAvailableAwsProfiles();
            Assert.True(listItems.Any(i => i.Name == name), $"Fail to find [{name}] profile");
            ProfilesComboBox.Select(name);
        }

        public void CheckSwitchAwsProfile()
        {
            List<AutomationElement> listItems = GetAvailableAwsProfiles();
            listItems.FirstOrDefault().DrawHighlight();
            ProfilesComboBox.Select(listItems.FirstOrDefault().Name);
            if (listItems.Count > 1)
            {
                listItems.LastOrDefault().DrawHighlight();
                ProfilesComboBox.Select(listItems.LastOrDefault().Name);
            }
        }

        public void TestProfileRadioButton()
        {
            SelectDefaultSDKChainRadioButton();
            SelectAWSProfileRadioButton();
        }

        private List<AutomationElement> GetAvailableAwsProfiles()
        {
            SwitchToDataSharingCategory();
            TestProfileRadioButton();
            ProfilesComboBox.DrawHighlight();
            ProfilesComboBox.Expand();
            Retry.WhileFalse(() =>
            {
                var listItems = FindAllDescendants(e => e.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem));
                return listItems?.Length > 0;
            }, timeout: TimeSpan.FromSeconds(2), throwOnTimeout: true, timeoutMessage: "Fail to get asw profile items");
            var listItems = FindAllDescendants(e => e.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem)).ToList();
            return listItems;
        }

        private void SwitchToDataSharingCategory()
        {
            DataUsageSharingTreeItem.Select();
            EnableMatricCheckbox.WaitUntilEnabled();
            if (EnableMatricCheckbox.IsChecked.HasValue && !EnableMatricCheckbox.IsChecked.Value)
            {
                EnableMatricCheckbox.IsChecked = true;
            }
        }

        public bool FindAwsProfileByname(string name) {
            List<AutomationElement> listItems = GetAvailableAwsProfiles();
            return listItems!=null && listItems.Any(l => l?.Name == name);
        }

        internal void CreateNewAwsProfile(string name, string assessKey, string secretKey)
        {
            var addNewProfileDialog = OpenNewAwsProfileDialog();
            addNewProfileDialog.SaveNewProfile(name, assessKey, secretKey);
            Retry.WhileNotNull(()=> FindFirstChild(e => e.ByName("Add a Named Profile").And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Window))),
                timeout:TimeSpan.FromSeconds(2),throwOnTimeout: true,timeoutMessage: "Fail to close [Add a New Profile] window by saving"
            );
        }

        internal void CheckNewAwsProfileValidation(string name, string assessKey, string secretKey)
        {
            var addNewProfileDialog = OpenNewAwsProfileDialog();
            addNewProfileDialog.SaveNewProfile(name, assessKey, secretKey);
            addNewProfileDialog.CheckInvalidProfileWarning();
        }

        internal AddNewProfileDialog OpenNewAwsProfileDialog()
        {
            AddProfileBtn.WaitUntilClickable();
            AddProfileBtn.Invoke();
            return Retry.Find(() => FindFirstChild(e => e.ByName("Add a Named Profile").And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Window))),

                new RetrySettings
                {
                    Timeout = TimeSpan.FromSeconds(2),
                    ThrowOnTimeout = true,
                    TimeoutMessage = "Fail to open add new profile window"
                }).As<AddNewProfileDialog>();
        }

        internal void VerifyToLearnMoreHyperlinkInDataSharing()
        {
            SwitchToDataSharingCategory();
            ToLearnMoreHyperLink.WaitUntilClickable();
            ToLearnMoreHyperLink.DrawHighlight();
            ToLearnMoreHyperLink.Click();
        }

        internal void VerifyDocumentHyperlinkAndCancelNewProfileBtn()
        {
            SwitchToDataSharingCategory();
            AddProfileBtn.WaitUntilClickable();
            AddProfileBtn.Invoke();
            var addNewProfileDialog = Retry.Find(() => FindFirstChild(e => e.ByName("Add a Named Profile").And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Window))),
                new RetrySettings
                {
                    Timeout = TimeSpan.FromSeconds(2),
                    ThrowOnTimeout = true,
                    TimeoutMessage = "Fail to open add new profile window"
                }).As<AddNewProfileDialog>();

            addNewProfileDialog.VerifyDocumentHyperlink();
            addNewProfileDialog.CancelBtn.WaitUntilClickable();
            addNewProfileDialog.CancelBtn.Invoke();
            Retry.WhileNotNull(() => FindFirstChild(e => e.ByName("Add a Named Profile").And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Window))),
                timeout: TimeSpan.FromSeconds(2), throwOnTimeout: true, timeoutMessage: "Fail to close [Add a New Profile] window by cancelbtn"
            );
        }

        internal void CancelOptionsSettings()
        {
            CancelBtn.WaitUntilEnabled();
            CancelBtn.WaitUntilClickable();
            CancelBtn.Invoke();
        }
    }
}
