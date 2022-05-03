using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using System;

namespace IDE_UITest.UI
{
    public class GetStartedWindow: ElementBase
    {
        public GetStartedWindow(FrameworkAutomationElementBase frameworkAutomationElement) : base(frameworkAutomationElement)
        {
        }

        public Button SaveBtn => WaitForElement(() => FindFirstChild(e => e.ByName(" Save ")).AsButton());
        public Button CanceleBtn => WaitForElement(() => FindFirstChild(e => e.ByName(" Cancel ")).AsButton());

        public ComboBox ProfilesComboBox => WaitForElement(() => FindFirstChild(e => e.ByAutomationId("Profiles").
            And(e.ByControlType(FlaUI.Core.Definitions.ControlType.ComboBox))).AsComboBox());

        internal CheckBox AgreeToShareCheckbox => WaitForElement(() => FindFirstChild(e => e.ByAutomationId("AgreeToShare").
         And(e.ByControlType(FlaUI.Core.Definitions.ControlType.CheckBox))).AsCheckBox());
        internal RadioButton DefaultProviderChainRadioButton => WaitForElement(() => FindFirstChild(e => e.ByName("Use the AWS SDK Default Provider Chain").
            And(e.ByControlType(FlaUI.Core.Definitions.ControlType.RadioButton))).AsRadioButton());
        internal RadioButton UseAWSProfileRadioButton => WaitForElement(() => FindFirstChild(e => e.ByName("Use AWS Profile").
            And(e.ByControlType(FlaUI.Core.Definitions.ControlType.RadioButton))).AsRadioButton());

        public void SelectAwsProfile(string profileName)
        {
            AgreeToShareCheckbox.WaitUntilEnabled();
            if (AgreeToShareCheckbox.IsChecked.HasValue && !AgreeToShareCheckbox.IsChecked.Value)
            {
                AgreeToShareCheckbox.IsChecked = true;
            }
            ProfilesComboBox.DrawHighlight();
            ProfilesComboBox.Expand();
            ProfilesComboBox.Select(profileName);
        }

        public void CheckAgreeToShareCheckbox()
        {
            AgreeToShareCheckbox.WaitUntilEnabled();
            AgreeToShareCheckbox.DrawHighlight();
            AgreeToShareCheckbox.IsChecked = false;
            AgreeToShareCheckbox.DrawHighlight();
            AgreeToShareCheckbox.IsChecked = true;
        }

        public void ClickDefaultProviderChainRadioButton()
        {
            DefaultProviderChainRadioButton.DrawHighlight();
            DefaultProviderChainRadioButton.Click();
        }

        public void ClickSaveButtonWithoutProfileSelected()
        {
            SaveBtn.DrawHighlight();
            SaveBtn.Invoke();
            var noProfileSelectedWarning = Retry.Find(() => FindFirstChild(e => e.ByAutomationId("WarningBar").
              And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Text))),
                new RetrySettings
                {
                    Timeout = TimeSpan.FromSeconds(5),
                    Interval = TimeSpan.FromSeconds(1),
                    ThrowOnTimeout = true,
                    TimeoutMessage = $"Fail to Get No Profile Selected Warning!"
                });
            noProfileSelectedWarning.DrawHighlight();
        }

        public void CancelGetStartedWindow()
        {
            CanceleBtn.DrawHighlight();
            CanceleBtn.Invoke();
        }

        public void ClickUseAWSProfileRadioButton()
        {
            UseAWSProfileRadioButton.DrawHighlight();
            UseAWSProfileRadioButton.Click();
        }

        public void AddAndSelectANewProfile(string name, string assessKey, string secretKey)
        {
            AddNewProfile(name, assessKey, secretKey);
            SelectAwsProfile(name);
            ClickSaveButton();
        }

        private void AddNewProfile(string name, string assessKey, string secretKey)
        {
            var addProfileBtn = WaitForElement(() => FindFirstChild(e => e.ByName("Add Named Profile"))).AsButton();
            addProfileBtn.DrawHighlight();
            addProfileBtn.Invoke();
            var addNewProfileDialog = Retry.Find(() => FindFirstChild(e => e.ByName("Add a Named Profile").And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Window))),

                new RetrySettings
                {
                    Timeout = TimeSpan.FromSeconds(10),
                    ThrowOnTimeout = true,
                    TimeoutMessage = "Fail to open add new profile window"
                }).As<AddNewProfileDialog>();

            addNewProfileDialog.SaveNewProfile(name, assessKey, secretKey);
            Retry.WhileNotNull(() => FindFirstChild(e => e.ByName("Add a Named Profile").And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Window))),
                timeout: TimeSpan.FromSeconds(5), throwOnTimeout: true, timeoutMessage: "Fail to close [Add a New Profile] window by saving"
            );
        }

        private void ClickSaveButton()
        {
            SaveBtn.DrawHighlight();
            SaveBtn.Invoke();
        }
    }
}
