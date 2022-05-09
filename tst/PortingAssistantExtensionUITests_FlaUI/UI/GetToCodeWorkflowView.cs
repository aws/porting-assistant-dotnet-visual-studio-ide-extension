using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using System;
using Xunit;

namespace IDE_UITest.UI
{
    public class GetToCodeWorkflowView: ElementBase
    {
        public GetToCodeWorkflowView(FrameworkAutomationElementBase frameworkAutomationElement) : base(frameworkAutomationElement)
        {
        }

        public Button ContinueWithoutCodeBtn => WaitForElement(() => FindFirstChild(e => e.ByName("Continue without code")).AsButton(), 10);
        public Window ScrollViewer1Pane => WaitForElement(() => FindFirstChild(e => e.ByAutomationId("ScrollViewer_1").
            And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Pane))).AsWindow(), 10);
        public Button OpenProjOrSlnBtn => WaitForElement(() => ScrollViewer1Pane.FindFirstChild(e => e.ByName("Open a _project or solution")).AsButton(), 10);
        public void InvokeContinueWithoutCodeBtn() {
            ContinueWithoutCodeBtn.WaitUntilEnabled();
            ContinueWithoutCodeBtn.DrawHighlight();
            ContinueWithoutCodeBtn.Invoke();
        }

        internal void LoadSolution(string solutionPath)
        {
            OpenProjOrSlnBtn.WaitUntilEnabled();
            OpenProjOrSlnBtn.DrawHighlight();
            OpenProjOrSlnBtn.Invoke();
            var fileBrowserWin = Retry.Find(() => Parent.FindFirstChild(e => e.ByName("Open Project/Solution").
                And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Window))),
                new RetrySettings
                {
                    Timeout = TimeSpan.FromSeconds(15),
                    Interval = TimeSpan.FromMilliseconds(500)
                }).As<FileBrowserWindow>();
            Assert.True(fileBrowserWin!= null, "File browser window should appear");
            fileBrowserWin.SelectSolution(solutionPath);
        }
    }
}
