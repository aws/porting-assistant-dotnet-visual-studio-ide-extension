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
    public class ProfileTests : VisualStudioSession
    {
        private const string VS2019AppId = @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\Common7\IDE\devenv.exe";
        // protected static WindowsDriver<WindowsElement> session;
        [TestMethod]
        public void InvalidProfileTest()
        {
            AddAWSProfile("invalidProfile", "test", "test");
            WaitForElement(session, "//Window[@Name=\"Add a Named Profile\"][@ClassName=\"Window\"]/Text[@AutomationId=\"WarningValidation\"][@Name=\"Please provide a valid aws profile\"]/Text[@Name=\"Please provide a valid aws profile\"][@ClassName=\"TextBlock\"]", 10);
            session.FindElementByXPath("//Button[@ClassName=\"Button\"][@Name=\"Cancel\"]").Click();
            session.FindElementByXPath("//Button[@ClassName=\"Button\"][@Name=\"OK\"]").Click();
        }

        [TestMethod]
        public void TestSwitchValidProfiles()
        {
            string homeFolder = Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
            string credsFile = homeFolder + "/.aws/credentials";
            string originalProfileName = "";
            string accessKey = "";
            string secretAccessKey = "";
            using (StreamReader reader = new StreamReader(credsFile))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains("["))
                    {
                        originalProfileName = line.Substring(1, line.Length - 2);
                        accessKey = reader.ReadLine().Split('=')[1];
                        secretAccessKey = reader.ReadLine().Split('=')[1];
                        break;
                    }

                }
            }
            accessKey = string.Concat(accessKey.Where(c => !char.IsWhiteSpace(c)));
            secretAccessKey = string.Concat(secretAccessKey.Where(c => !char.IsWhiteSpace(c)));
            AddAWSProfile("TestProfile", accessKey, secretAccessKey);
            session.FindElementByXPath("//Window[@Name =\"Options\"][@ClassName=\"#32770\"]/Button[@Name=\"OK\"][@ClassName=\"Button\"]").Click();
            // Switch back to default
            ClickPortingAssistantMenuElement("Settings...");
            session.FindElementByName("Data usage sharing").Click();
            var profilesBox = session.FindElementByAccessibilityId("Profiles");
            profilesBox.Click();
            profilesBox.Click();
            session.FindElementByXPath($"//Window[@ClassName=\"Popup\"]/ListItem[@Name=\"{originalProfileName}\"][@ClassName=\"ListBoxItem\"]").Click();
            session.FindElementByXPath("//Button[@ClassName=\"Button\"][@Name=\"OK\"]").Click();
        }

        [TestInitialize]
        public void ClassInitialize()
        {
            Assert.IsTrue(File.Exists(@"C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\Common7\IDE\devenv.exe"));
            var solutionPath = $"{testSolutionsDir}\\mvcmusicstore\\sourceCode\\mvcmusicstore\\MvcMusicStore.sln";
            Setup(solutionPath);
        }

        [TestCleanup]
        public void ClassCleanup()
        {
            TearDown();
        }

    }
}
