using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace IDE_UITest.UI
{
    public class VsixInstallerView : ElementBase
    {
        public VsixInstallerView(FrameworkAutomationElementBase frameworkAutomationElement) : base(frameworkAutomationElement)
        {

        }

        public void InstallVsix()
        {
            WaitInstallationButton();
            WaitTillInstallationFinished();
        }

        private void WaitInstallationButton(int timeoutSec = 120)
        {
            var VsixInstallButton = Retry.Find(() => FindFirstChild(e => e.ByName("Install")
                .And(e.ByClassName("Button"))),
                new RetrySettings
                {
                    Timeout = TimeSpan.FromSeconds(timeoutSec),
                    Interval = TimeSpan.FromSeconds(5),
                    ThrowOnTimeout = true,
                    TimeoutMessage = $"Fail to finish installation within {timeoutSec} seconds"
                });
            VsixInstallButton.DrawHighlight();
            VsixInstallButton.Click();
        }

        private void WaitTillInstallationFinished(int timeoutSec = 300)
        {
            var InstallationResultText = Retry.Find(() => FindFirstChild(e => e.ByControlType(FlaUI.Core.Definitions.ControlType.Text)
                .And(e.ByName("Install Complete"))),
                new RetrySettings
                {
                    Timeout = TimeSpan.FromSeconds(timeoutSec),
                    Interval = TimeSpan.FromSeconds(5),
                    ThrowOnTimeout = true,
                    TimeoutMessage = $"Fail to finish installation within {timeoutSec} seconds"
                });
            InstallationResultText.DrawHighlight();
            var VsixInstallButton = WaitForElement(() => FindFirstChild(e => e.ByName("Close")
                .And(e.ByClassName("Button"))), 10).AsButton();
            VsixInstallButton.DrawHighlight();
            VsixInstallButton.Click();
        }
    }
}
