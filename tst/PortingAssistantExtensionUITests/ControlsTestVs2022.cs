using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using System.Diagnostics;
using OpenQA.Selenium.Interactions;

namespace PortingAssistantExtensionUITests
{
    /*
    * Editor
        * Light bulb actions - Covered because recommended actions are covered by porting
    * Solution Explorer (right-click actions)
        * Full assessment
        * Port Solution
        * Port Project
    * Menu Items
        * Full assessment
        * Enable Incremental assessment
        * Port Solution
        * Port Project
        * Settings
        * Support
        * Documentation
    */
    /// <summary>
    /// Tests the controls for the IDE extension
    /// </summary>
    [TestClass]
    public class ControlsTestVs2022 : VisualStudioSession 
    {
        public ControlsTestVs2022() : base(@"C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\devenv.exe") { }

        [TestMethod]
        public void TestChangeTargetFramework()
        {
            SelectTargetFramework();
        }

        [TestMethod]
        public void TestRightClickActions()
        {
            RightClickRunAssessment();
            RightClickRunPorting();
        }

        [TestMethod]
        public void TestSupportActions()
        {
            ClickPortingAssistantMenuElement("Support");
            new Actions(session).SendKeys(Keys.Escape).Perform();
            ClickPortingAssistantMenuElement("Documentation");
        }

        [TestMethod]
        public void Net6ExistsVS2022()
        {
            ClickPortingAssistantMenuElement("Settings...");
            session.FindElementByName("General").Click();
            // This not a typo, the automation id is missing the last t
            var frameworksBox = session.FindElementByAccessibilityId("TargeFrameworks");
            frameworksBox.Click();
            session.FindElementByXPath($"//Window[@ClassName=\"Popup\"]/ListItem[@Name=\"net6.0\"][@ClassName=\"ListBoxItem\"]").Click();
            session.FindElementByXPath("//Button[@ClassName=\"Button\"][@Name=\"OK\"]").Click();
        }

        [TestInitialize]
        public void ClassInitialize()
        {
            var solutionPath = $"{testSolutionsDir}\\mvcmusicstore\\sourceCode\\mvcmusicstore\\MvcMusicStore.sln";
            Setup(solutionPath);
        }

        [TestCleanup]
        public void ClassCleanup()
        {
            TearDown();
        }

        private static void RightClickRunAssessment()
        {
            GoToFile("AccountController.cs");
            
            session.FindElementByName("View").Click();
            session.FindElementByXPath($"//MenuItem[starts-with(@Name, \"Solution Explorer\")]").Click();

            string arrowDownToAssess = "";
            for (int i = 0; i < 12; i++)
            {
                arrowDownToAssess += Keys.ArrowDown;
            }

            var solutionElement = session.FindElementByXPath("//Pane[@ClassName=\"GenericPane\"][@Name=\"Solution Explorer\"]/Tree[@Name=\"Solution Explorer\"][@AutomationId=\"SolutionExplorer\"]/TreeItem[@ClassName=\"TreeViewItem\"][starts-with(@Name,\"Solution\")]");
            var builder = new Actions(session);
            builder.ContextClick(solutionElement).SendKeys(arrowDownToAssess).SendKeys(Keys.Enter).Perform();
            WaitForElement("//Pane[starts-with(@Name,\"Assessment successful. You can view the assessment results in th\")]", 120);
        }

        private static void RightClickRunPorting()
        {
            // assumes solution explorer is up from assessment test
            string keys = "";
            for (int i = 0; i < 14; i++) 
            {
                keys += Keys.ArrowDown; 
            }
            var solutionElement = session.FindElementByXPath("//Pane[@ClassName=\"GenericPane\"][@Name=\"Solution Explorer\"]/Tree[@Name=\"Solution Explorer\"][@AutomationId=\"SolutionExplorer\"]/TreeItem[@ClassName=\"TreeViewItem\"][starts-with(@Name,\"Solution\")]");
            var builder = new Actions(session);
            builder.ContextClick(solutionElement).SendKeys(keys).SendKeys(Keys.Enter).Perform();
        }

        private static bool IsProcessRunning(string processName) 
        {
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length > 0)
            {
                foreach(var process in processes)
                {
                    process.Kill();
                }
                return true;
            }
            return false;
        }
    }
}
