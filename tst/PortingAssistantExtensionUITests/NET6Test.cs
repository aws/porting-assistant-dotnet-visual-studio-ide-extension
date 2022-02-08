using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Appium.Windows;
using System.Threading;
using System.IO;
using System.Linq;
using OpenQA.Selenium;

namespace PortingAssistantExtensionUITests
{
    [TestClass]
    public class NET6Test : VisualStudioSession
    {
        public NET6Test() : base(@"C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\Common7\IDE\devenv.exe") { }
        [TestMethod]
        [ExpectedException(typeof(System.InvalidOperationException))]
        public void Net6DoesNotExistsVS2019()
        {
            ClickPortingAssistantMenuElement("Settings...");
            session.FindElementByName("General").Click();
            // This not a typo, the automation id is missing the last t
            var frameworksBox = session.FindElementByAccessibilityId("TargeFrameworks");
            frameworksBox.Click();
            session.FindElementByXPath($"//Window[@ClassName=\"Popup\"]/ListItem[@Name=\"net6.0\"][@ClassName=\"ListBoxItem\"]").Click();
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
            session.FindElementByXPath("//Button[@ClassName=\"Button\"][@Name=\"Cancel\"]").Click();
            TearDown();
        }
    }
}
