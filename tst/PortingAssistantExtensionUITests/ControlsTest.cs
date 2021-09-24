using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using OpenQA.Selenium;
using System;
using System.Threading;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Appium.Service;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System.Configuration;

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
    public class ControlsTest : VisualStudioSession 
    {
        [TestMethod]
        public void RightClickActions()
        {
            RightClickRunAssessment();
            RightClickRunPorting();
        }

        public void SupportActions()
        {
            TestSupportActions();
        }

        [TestInitialize]
        public void ClassInitialize()
        {
            
            Setup(@"C:\ide-ui-test-solutions\mvcmusicstore\sourceCode\mvcmusicstore\MvcMusicStore.sln");
        }

        [TestCleanup]
        public void ClassCleanup()
        {
            TearDown();
        }

        private static void CheckLightBulbAction()
        {
            // Filter on action messages 
            session.FindElementByAccessibilityId("errorcode").Click();
            session.FindElementByAccessibilityId("Code column filter").Click();
            session.FindElementByXPath("//Window[@ClassName=\"Popup\"]/CheckBox[@Name=\"(Select All)\"]").Click();
            session.FindElementByXPath("//Window[@ClassName=\"Popup\"]/CheckBox[starts-with(@Name, \"PA0002\")]").Click();
            // Click on message in error list
            var actionText = session.FindElementByXPath("//Text[@Name=\"Replace System.Web.Mvc namespace with Microsoft.AspNetCore.Mvc.\"][starts-with(@AutomationId,\"Replace System.Web.Mvc namespace with Microsoft.AspNetCore.Mvc\")]");
            actionText.Click();
            actionText.Click();

            // Click lightbulb and action
            // todo: for some reason actions aren't coming up in the test version of visual studio
            //session.FindElementByXPath("//Window[@ClassName=\"Popup\"]/Image[@ClassName=\"Image\"]").Click();
            //session.FindElementByXPath("//Menu[@Name=\"Light Bulb Menu\"][@AutomationId=\"LightBulbButtonMenu\"]/MenuItem[starts-with(@Name,\"Replace System.Web.Mvc namespace with Microsoft.AspNetCore.Mvc..\")][starts-with(@AutomationId,\"Replace System.Web.Mvc namespace with Microsoft.AspNetCore.Mvc\")]/Text[@ClassName=\"TextBlock\"][@Name=\"Replace System.Web.Mvc namespace with Microsoft.AspNetCore.Mvc.\"]").Click();
            // Verify file action has been applied
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

            WaitForElement("//Pane[starts-with(@Name,\"Assessment successful. You can view the assessment results in th\")]", 60);
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

        private static void TestSupportActions()
        {
            Console.WriteLine(desktopSession.WindowHandles);
            Console.WriteLine(desktopSession.WindowHandles.Count);
            ClickPortingAssistantMenuElement("Support");
            //WaitForElement("//Window[@Name=\"How do you want to open this?\"][@AutomationId=\"Popup Window\"]/Text[@Name=\"How do you want to open this?\"][@AutomationId=\"HeadText\"]");
            Console.WriteLine(desktopSession.WindowHandles.Count);
            new Actions(session).SendKeys(Keys.Escape).Perform();
            ClickPortingAssistantMenuElement("Documentation");
            //WaitForDesktopElement("/ToolBar[@Name=\"Browser tabs\"][@AutomationId=\"TabsToolbar\"]/Tab[@AutomationId=\"tabbrowser-tabs\"]/TabItem[starts-with(@Name,\"What is Porting Assistant for .NET? - Porting Assistant for .NET\")]");

        }
    }
}
