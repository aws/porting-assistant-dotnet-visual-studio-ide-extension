using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace IDE_UITest.UI
{
    public class PublishDialog : ElementBase
    {
        public PublishDialog(FrameworkAutomationElementBase frameworkAutomationElement) : base(frameworkAutomationElement)
        {
        }
        internal AutomationElement ProfileTargetViewControl => WaitForElement(() => FindFirstChild(e => e.ByClassName("ProfileTargetViewControl").And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Custom))));
        internal AutomationElement AvailablePulishTargetsList => WaitForElement(() => ProfileTargetViewControl.FindFirstChild(e => e.ByAutomationId("TargetList").And(e.ByControlType(FlaUI.Core.Definitions.ControlType.List))));
        internal ListBoxItem FolderListItem => WaitForElement(() => AvailablePulishTargetsList.FindFirstChild(e => e.ByName("Folder").And(e.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem))).AsListBoxItem());
        internal Button NextBtn => WaitForElement(() => FindFirstChild(e => e.ByName("Next").And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Button))).AsButton());
        internal Button FinishBtn => WaitForElement(() => FindFirstChild(e => e.ByName("Finish").And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Button))).AsButton());

        public void SelectPublishTargetType()
        {
            FolderListItem.DrawHighlight();
            FolderListItem.Select();
            NextBtn.DrawHighlight();
            NextBtn.Invoke();
        }

        public void SelectPublishTargetLocation(string location)
        {
            var localFolderViewControlPanel = Retry.Find(() => FindFirstChild(e => e.ByClassName("LocalFolderViewControl").
              And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Custom))),
                new RetrySettings
                {
                    Timeout = TimeSpan.FromSeconds(30),
                    Interval = TimeSpan.FromSeconds(1),
                    ThrowOnTimeout = true,
                    TimeoutMessage = $"Fail to Get local Folder View Control Panel!"
                });
            localFolderViewControlPanel.DrawHighlight();
            var pulishUrlPanel = Retry.Find(() => localFolderViewControlPanel.FindFirstChild(e => e.ByAutomationId("PublishUrl").
              And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Custom))),
                new RetrySettings
                {
                    Timeout = TimeSpan.FromSeconds(30),
                    Interval = TimeSpan.FromSeconds(1),
                    ThrowOnTimeout = true,
                    TimeoutMessage = $"Fail to Get Publish Url Panel!"
                });
            pulishUrlPanel.DrawHighlight();
            var folderLocationTextBox = WaitForElement(() => pulishUrlPanel.FindFirstChild(e => e.ByName("Folder location").
                And(e.ByClassName("TextBox")))).AsTextBox();
            folderLocationTextBox.WaitUntilEnabled();
            folderLocationTextBox.DrawHighlight();
            folderLocationTextBox.Enter(location);
            FinishBtn.WaitUntilClickable();
            FinishBtn.DrawHighlight();
            FinishBtn.Invoke();
        }
    }
}
