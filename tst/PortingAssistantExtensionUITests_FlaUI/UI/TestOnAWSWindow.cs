using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using System;
using System.Linq;

namespace IDE_UITest.UI
{
    public class TestOnAWSWindow : ElementBase
    {
        public TestOnAWSWindow(FrameworkAutomationElementBase frameworkAutomationElement) : base(frameworkAutomationElement)
        {

        }

        internal ComboBox AwsProfileComboBox => WaitForElement(() => FindFirstChild(e => e.ByAutomationId("AwsProfileComboBox")
            .And(e.ByControlType(FlaUI.Core.Definitions.ControlType.ComboBox)))).AsComboBox();
        internal TextBox BuildArtifactsDirBrowseTextBox => WaitForElement(() => FindFirstChild(e => e.ByAutomationId("BuildArtifactsDirBrowseTextBox")
            .And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Edit)))).AsTextBox();
        internal TextBox DeploymentNameTextBox => WaitForElement(() => FindFirstChild(e => e.ByAutomationId("DeploymentNameTextBox")
            .And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Edit)))).AsTextBox();
        internal Button TestOnAWSBtn => WaitForElement(() => FindFirstChild(e => e.ByName("Test on AWS")
            .And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Button)))).AsButton();
        internal Button CancelBtn => WaitForElement(() => FindFirstChild(e => e.ByName("Cancel")
            .And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Button)))).AsButton();

        internal void ClickTestOnAWSBtn()
        {
            TestOnAWSBtn.DrawHighlight();
            TestOnAWSBtn.Invoke();
        }

        internal void ClickCancelBtn()
        {
            CancelBtn.DrawHighlight();
            CancelBtn.Invoke();
        }

        public void CheckAWSProfileValidation()
        {
            AwsProfileComboBox.DrawHighlight();
            AwsProfileComboBox.Expand();
            AwsProfileComboBox.Collapse();
            ClickTestOnAWSBtn();
            var profileErrorHint = Retry.Find(() => FindFirstChild(e => e.ByName("ERROR: Please select AWS profile").
              And(e.ByAutomationId("ProfileErrorHint"))),
                new RetrySettings
                {
                    Timeout = TimeSpan.FromSeconds(60),
                    Interval = TimeSpan.FromSeconds(1),
                    ThrowOnTimeout = true,
                    TimeoutMessage = $"Fail to Get Profile Error Hint!"
                });
            profileErrorHint.DrawHighlight();
            ClickCancelBtn();
        }

        public void CheckBuildArtifactsDirectoryValidation(string inputDir)
        {
            BuildArtifactsDirBrowseTextBox.DrawHighlight();
            BuildArtifactsDirBrowseTextBox.Enter(inputDir);
            ClickTestOnAWSBtn();
            var buildArtifactsDirBrowserButtonErrorHint = Retry.Find(() => FindFirstChild(
                e => e.ByName("ERROR: Build folder with artifacts not found").
              And(e.ByAutomationId("BuildArtifactsDirBrowserButtonErrorHint"))),
                new RetrySettings
                {
                    Timeout = TimeSpan.FromSeconds(60),
                    Interval = TimeSpan.FromSeconds(1),
                    ThrowOnTimeout = true,
                    TimeoutMessage = $"Fail to Get Build folder with artifacts Error Hint!"
                });
            buildArtifactsDirBrowserButtonErrorHint.DrawHighlight();
            ClickCancelBtn();
        }

        public void CheckDeploymentNameValidation(string inputDeploymentName)
        {
            DeploymentNameTextBox.DrawHighlight();
            DeploymentNameTextBox.Enter(inputDeploymentName);
            ClickTestOnAWSBtn();
            var deploymentNameErrorHint = Retry.Find(() => FindFirstChild(
                e => e.ByName("ERROR: Please fix application name").
              And(e.ByAutomationId("DeploymentNameErrorHint"))),
                new RetrySettings
                {
                    Timeout = TimeSpan.FromSeconds(60),
                    Interval = TimeSpan.FromSeconds(1),
                    ThrowOnTimeout = true,
                    TimeoutMessage = $"Fail to Get Deployment Name Error Hint!"
                });
            deploymentNameErrorHint.DrawHighlight();
            ClickCancelBtn();
        }

        public void RunDeploymentWithValidValues(string publishLocation)
        {
            AwsProfileComboBox.DrawHighlight();
            AwsProfileComboBox.Expand();
            Retry.WhileFalse(() =>
            {
                var listItems = FindAllDescendants(e => e.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem));
                return listItems?.Length > 0;
            }, timeout: TimeSpan.FromSeconds(2), throwOnTimeout: true, timeoutMessage: "Fail to get aws profile items");
            var listItems = FindAllDescendants(e => e.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem)).ToList();

            AwsProfileComboBox.Select(listItems.FirstOrDefault().Name);

            BuildArtifactsDirBrowseTextBox.DrawHighlight();
            BuildArtifactsDirBrowseTextBox.Enter(publishLocation);

            DeploymentNameTextBox.DrawHighlight();
            DeploymentNameTextBox.Enter("TestCoreMvcApp");

            ClickTestOnAWSBtn();
        }
    }
}
