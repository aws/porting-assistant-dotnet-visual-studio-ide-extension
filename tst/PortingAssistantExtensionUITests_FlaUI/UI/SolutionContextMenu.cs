using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using System;
using System.Collections.Generic;
using System.Text;

namespace IDE_UITest.UI
{
    public class SolutionContextMenu : ElementBase
    {
        public SolutionContextMenu(FrameworkAutomationElementBase frameworkAutomationElement) : base(frameworkAutomationElement)
        {
        }

        private Menu popUpMContextMenu => WaitForElement(() => FindFirstDescendant(e => e.ByName("Solution").
            And(e.ByClassName("ContextMenu")).And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Menu)))).AsMenu();

        public void ClickContextMenuByName(string name) 
        {
            var menuItem  = WaitForElement(() => popUpMContextMenu.FindFirstDescendant(e => e.ByName(name).
                And(e.ByClassName("MenuItem")).And(e.ByControlType(FlaUI.Core.Definitions.ControlType.MenuItem)))).AsMenuItem();
            menuItem.WaitUntilClickable();
            menuItem.DrawHighlight();
            menuItem.Click();
        }

        public MenuItem GetContextMenuItemByName(string name)
        { 
             return popUpMContextMenu.FindFirstDescendant(e => e.ByName(name).
                And(e.ByClassName("MenuItem")).And(e.ByControlType(FlaUI.Core.Definitions.ControlType.MenuItem))).AsMenuItem();
        }
    }
}
