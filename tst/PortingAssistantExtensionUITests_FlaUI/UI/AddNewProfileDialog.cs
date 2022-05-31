using FlaUI.Core;
using FlaUI.Core.AutomationElements;

namespace IDE_UITest.UI
{
    public class AddNewProfileDialog : ElementBase
    {
        public AddNewProfileDialog(FrameworkAutomationElementBase frameworkAutomationElement) : base(frameworkAutomationElement)
        {
        }
        internal TextBox ProfileNameTxt => WaitForElement(() => FindFirstChild(e => e.ByAutomationId("ProfileName")).AsTextBox());
        internal TextBox AccesskeyIDTxt => WaitForElement(() => FindFirstChild(e => e.ByAutomationId("AccesskeyID")).AsTextBox());
        internal TextBox SecretAccessKeyTxt => WaitForElement(() => FindFirstChild(e => e.ByAutomationId("secretAccessKey")).AsTextBox());
        internal TextBox SessionTokenTxt => WaitForElement(() => FindFirstChild(e => e.ByAutomationId("sessionToken")).AsTextBox());

        public Button CancelBtn => WaitForElement(() => FindFirstChild(e => e.ByName("Cancel")).AsButton());
        internal Button SaveBtn => WaitForElement(() => FindFirstChild(e => e.ByName("Save Profile")).AsButton());
        internal AutomationElement DocumentHyperLink => WaitForElement(() => FindFirstDescendant(e =>
            e.ByName("Login to the IAM Users page in the AWS Console").And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Hyperlink))));

        public void SaveNewProfile(string name, string id, string secret) 
        {
            ProfileNameTxt.WaitUntilEnabled();
            ProfileNameTxt.Enter(name);
            AccesskeyIDTxt.WaitUntilEnabled();
            AccesskeyIDTxt.Enter(id);
            SecretAccessKeyTxt.WaitUntilEnabled();
            SecretAccessKeyTxt.Enter(secret);
            ClickSaveButton();
        }

        internal void VerifyDocumentHyperlink()
        {
            DocumentHyperLink.WaitUntilClickable();
            DocumentHyperLink.DrawHighlight();
            DocumentHyperLink.Click();
        }

        public void ClickSaveButton()
        {
            SaveBtn.WaitUntilClickable();
            SaveBtn.DrawHighlight();
            SaveBtn.Invoke();
        }

        internal void CheckInvalidProfileWarning()
        {
            SessionTokenTxt.WaitUntilEnabled();
            SessionTokenTxt.Enter("invalid");
            ClickSaveButton();
            var ProfileValidationWarning = WaitForElement(() => FindFirstChild(e =>
             e.ByAutomationId("WarningValidation").And(e.ByName("Please provide a valid aws profile"))), 10);
            ProfileValidationWarning.DrawHighlight();
        }
    }
}
