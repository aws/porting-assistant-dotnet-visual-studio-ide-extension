using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace IDE_UITest.UI
{
    public class ErrorListTabItem : ElementBase
    {
        public ErrorListTabItem(FrameworkAutomationElementBase frameworkAutomationElement) : base(frameworkAutomationElement)
        {
        }

        public AutomationElement ErrorListParentPane => WaitForElement(() => FindFirstChild(e => e.ByName("Error List")
            .And(e.ByClassName("ViewPresenter").And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Pane)))));
        public AutomationElement SearchControlGroup => WaitForElement(() => ErrorListParentPane.FindFirstChild(e => e.ByAutomationId("SearchControl")
            .And(e.ByClassName("SearchControl").And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Group)))));

        
        public TextBox SearchControlTxt => WaitForElement(() => SearchControlGroup.FindFirstChild(e => e.ByAutomationId("PART_SearchBox")
            .And(e.ByClassName("TextBox").And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Edit))))).AsTextBox();

        public Button SearchControlBtn => WaitForElement(() => SearchControlGroup.FindFirstChild(e => e.ByAutomationId("PART_SearchButton")
            .And(e.ByClassName("Button")))).AsButton();

        public AutomationElement ErrorListPane => WaitForElement(() => ErrorListParentPane.FindFirstChild(e => e.ByName("Error List")
            .And(e.ByClassName("GenericPane").And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Pane)))));

        public AutomationElement ErrorsTable => WaitForElement(() => ErrorListPane.FindFirstDescendant(e => e.ByAutomationId("Tracking List View")
           .And(e.ByClassName("ListView").And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Table)))));
        


        public void SearchPAErrorWarningItems()
        {
            SearchControlTxt.WaitUntilEnabled();
            SearchControlTxt.Enter("PA000");
            SearchControlTxt.DrawHighlight();
            Retry.WhileFalse(() => {

                var items = ErrorsTable.FindAllChildren(e => e.ByClassName("ListViewItem").
                     And(e.ByControlType(FlaUI.Core.Definitions.ControlType.DataItem)));
                return items.Length > 0;
            }, timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(500),
               throwOnTimeout: true, timeoutMessage: "Fail to find Warning/Error in Error List view"
            );
        }
    }
}
