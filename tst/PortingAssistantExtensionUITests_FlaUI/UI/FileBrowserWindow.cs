using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using System;
using System.Collections.Generic;
using System.Text;

namespace IDE_UITest.UI
{
    public class FileBrowserWindow : ElementBase
    {
        public FileBrowserWindow(FrameworkAutomationElementBase frameworkAutomationElement) : base(frameworkAutomationElement)
        {
        }

        public Button OpenBtn => WaitForElement(() => FindFirstChild(e => e.ByName("Open")).AsButton());

        public ComboBox FileNameComboBox => WaitForElement(() => FindFirstChild(e => e.ByName("File name:").
            And(e.ByControlType(FlaUI.Core.Definitions.ControlType.ComboBox))).AsComboBox());
        
        public TextBox FileNameTxt => WaitForElement(() => FileNameComboBox.FindFirstChild(e => e.ByName("File name:")).AsTextBox());

        public void SelectSolution(string solutionPath)
        {
            FileNameTxt.WaitUntilEnabled();
            FileNameTxt.Enter(solutionPath);
            OpenBtn.DrawHighlight();
            OpenBtn.Invoke();
        }
    }
}
