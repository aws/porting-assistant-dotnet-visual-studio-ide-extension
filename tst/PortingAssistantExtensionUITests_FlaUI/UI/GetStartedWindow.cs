using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace IDE_UITest.UI
{
    public class GetStartedWindow: ElementBase
    {
        public GetStartedWindow(FrameworkAutomationElementBase frameworkAutomationElement) : base(frameworkAutomationElement)
        {
        }

        public Button SaveBtn => WaitForElement(() => FindFirstChild(e => e.ByName(" Save ")).AsButton());

        public ComboBox ProfilesComboBox => WaitForElement(() => FindFirstChild(e => e.ByAutomationId("Profiles").
            And(e.ByControlType(FlaUI.Core.Definitions.ControlType.ComboBox))).AsComboBox());

        internal CheckBox AgreeToShareCheckbox => WaitForElement(() => FindFirstChild(e => e.ByAutomationId("AgreeToShare").
         And(e.ByControlType(FlaUI.Core.Definitions.ControlType.CheckBox))).AsCheckBox());
        
        public void SelectAwsProfile()
        {
            AgreeToShareCheckbox.WaitUntilEnabled();
            if (AgreeToShareCheckbox.IsChecked.HasValue && !AgreeToShareCheckbox.IsChecked.Value)
            {
                AgreeToShareCheckbox.IsChecked = true;
            }
            ProfilesComboBox.DrawHighlight();
            ProfilesComboBox.Expand();
            Retry.WhileFalse(() =>
            {
                var listItems = FindAllDescendants(e => e.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem));
                return listItems?.Length > 0;
            }, timeout: TimeSpan.FromSeconds(2), throwOnTimeout: true, timeoutMessage: "Fail to get asw profile items");
            var listItems = FindAllDescendants(e => e.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem)).ToList();

            ProfilesComboBox.Select(listItems.FirstOrDefault().Name);

            SaveBtn.Invoke();
        }
    }
}
