//******************************************************************************
//
// Copyright (c) 2017 Microsoft Corporation. All rights reserved.
//
// This code is licensed under the MIT License (MIT).
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//******************************************************************************

using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium;
using System;
using System.Threading;
using System.Diagnostics;
using Newtonsoft.Json;
using System.IO;
using System.IO.Compression;

namespace PortingAssistantExtensionUITests
{
    [TestClass]
    public class VisualStudioSession
    {
        protected const string WindowsApplicationDriverUrl = "http://127.0.0.1:4723";
        private const string VSAppId = @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\Common7\IDE\devenv.exe";
        protected const string winAppDriverExe = "C:\\Program Files (x86)\\Windows Application Driver\\WinAppDriver.exe";
        private const string testSolutionsDir = "C:\\ide-ui-test-solutions";
        private const string testSolutionsZip = "C:\\ide-ui-test-solutions.zip";
        private static bool firstTimeSetupRequired = true;

        protected static WindowsDriver<WindowsElement> session;
        protected static WindowsElement mainWindow;
        protected static WindowsDriver<WindowsElement> desktopSession;
        protected static Process winAppDriver;

        [AssemblyInitialize()]
        public static void AssemblyInit(TestContext context)
        {
            StartWinAppDriver();
            ResetTestSolutions();
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            Console.WriteLine("AssemblyCleanup");
            winAppDriver.Kill();
        }

        private static void ResetTestSolutions()
        {
            // assumes that test solutions are located in c:\ drive
            if (Directory.Exists(testSolutionsDir))
            {
                Directory.Delete(testSolutionsDir, true);
            }
            ZipFile.ExtractToDirectory(testSolutionsZip, "C:\\");
        }

        private static void StartWinAppDriver()
        {
            winAppDriver = new Process();
            winAppDriver.StartInfo = new ProcessStartInfo
            {
                FileName = winAppDriverExe,
                Arguments = "127.0.0.1 4723",
            };
            winAppDriver.Start();
        }

        public static void Setup(string testSolution)
        {
            if (session == null)
            {
                // Create a new session to launch Notepad application
                DesiredCapabilities appCapabilities = new DesiredCapabilities();
                appCapabilities.SetCapability("app", VSAppId);
                appCapabilities.SetCapability("appArguments", testSolution);
                session = new WindowsDriver<WindowsElement>(new Uri(WindowsApplicationDriverUrl), appCapabilities);
                Assert.IsNotNull(session);
                Assert.IsNotNull(session.SessionId);

                session.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
                if (firstTimeSetupRequired)
                {
                    FirstTimeVsSetup();
                }

                mainWindow = session.FindElementByAccessibilityId("VisualStudioMainWindow");
            }

            // Make sure errors windows is opened
            session.FindElementByName("View").Click();
            session.FindElementByXPath($"//MenuItem[starts-with(@Name, \"Error List\")]").Click();
        }

        private static void FirstTimeVsSetup()
        {
            // Fresh install of visual studio has setup screens we need to bypass.
            
            // Identify the current window handle. You can check through inspect.exe which window this is.
            var currentWindowHandle = session.CurrentWindowHandle;
            // Wait for 5 seconds or however long it is needed for the right window to appear/for the splash screen to be dismissed
            Thread.Sleep(TimeSpan.FromSeconds(5));

            session.FindElementByName("Not now, maybe later.").Click();
            Thread.Sleep(TimeSpan.FromSeconds(1));
            session.FindElementByName("Start Visual Studio").Click();
            Thread.Sleep(TimeSpan.FromSeconds(60));
            
            // Return all window handles associated with this process/application.
            // At this point hopefully you have one to pick from. Otherwise you can
            // simply iterate through them to identify the one you want.
            var allWindowHandles = session.WindowHandles;
            // Assuming you only have only one window entry in allWindowHandles and it is in fact the correct one,
            // switch the session to that window as follows. You can repeat this logic with any top window with the same
            // process id (any entry of allWindowHandles)
            session.SwitchTo().Window(allWindowHandles[0]);

            GetPortingAssistantMenuElement("Settings...");
            session.FindElementByAccessibilityId("TargeFrameworks").Click();
            session.FindElementByName(".netcoreapp3.1").Click();
            session.FindElementByName("Data usage sharing").Click();
            session.FindElementByAccessibilityId("Profiles").Click();
            session.FindElementByName("default").Click();
            session.FindElementByXPath("//Button[@ClassName=\"Button\"][@Name=\"OK\"]").Click();

            firstTimeSetupRequired = false;
        }

        public static void TearDown()
        {
            // Close the application and delete the session
            if (session != null)
            {
                session.Close();
                session.Quit();
                session = null;
            }
        }

        [TestInitialize]
        public void TestInitialize()
        {
            // Select all text and delete to clear the edit box
            //editBox.SendKeys(Keys.Control + "a" + Keys.Control);
            //editBox.SendKeys(Keys.Delete);
            //Assert.AreEqual(string.Empty, editBox.Text);
        }

        protected static void GoToFile(string fileName)
        {
            session.FindElementByName("Edit").Click();
            session.FindElementByXPath("//Window[@ClassName=\"Popup\"]/MenuItem[@ClassName=\"MenuItem\"][@Name=\"Go To\"]").Click();
            session.FindElementByXPath("//Window[@ClassName=\"Popup\"]/Window[@ClassName=\"Popup\"]/MenuItem[@ClassName=\"MenuItem\"][@Name=\"Go To File...\"]").Click(); ;
            var searchBox = session.FindElementByAccessibilityId("PART_SearchBox");
            searchBox.SendKeys(fileName);
            // give time for search results to populate to increase chance of correct file
            Thread.Sleep(TimeSpan.FromSeconds(1));
            searchBox.SendKeys(Keys.Enter);
        }

        protected static void GetPortingAssistantMenuElement(string menuItem)
        {
            session.FindElementByName("Extensions").Click();
            session.FindElementByXPath("//Window[@ClassName=\"Popup\"]/MenuItem[@ClassName=\"MenuItem\"][@Name=\"Porting Assistant For .Net\"]").Click();
            
            if (!string.IsNullOrEmpty(menuItem))
            {
                session.FindElementByXPath($"//Window[@ClassName=\"Popup\"]/MenuItem[@ClassName=\"MenuItem\"][@Name=\"{menuItem}\"]").Click();
            }
        }

        protected static void StartFullSolutionAssessment()
        {
            GetPortingAssistantMenuElement("Run Full Assessment with Porting Assistant");
        }

        protected static void PortSolution()
        {
            // Menu Action
            GetPortingAssistantMenuElement("Port Solution to .NET Core with Porting Assistant");

            // Check apply porting actions
            session.FindElementByAccessibilityId("ApplyPortActionCheck").Click();
            session.FindElementByXPath("//Button[@ClassName=\"Button\"][@Name=\"Port\"]/Text[@ClassName=\"TextBlock\"][@Name=\"Port\"]").Click();

            // Wait for finish
            WaitForElement("//Window[@ClassName=\"#32770\"][@Name=\"Microsoft Visual Studio\"]/Button[@ClassName=\"Button\"][@Name=\"OK\"]", 180); 

            // Reload projects
            session.FindElementByXPath("//Button[@ClassName=\"Button\"][@Name=\"OK\"]").Click();
            session.FindElementByXPath("//Button[@ClassName=\"Button\"][@Name=\"Reload All\"]").Click();
        }

        protected static bool WaitForElement(string xPath, int timeout = 60)
        {
            return WaitForElement(session, xPath, timeout);
        }

        protected static bool WaitForDesktopElement(string xPath, int timeout = 60)
        {
            return WaitForElement(desktopSession, xPath, timeout);
        }
        protected static bool WaitForElement(WindowsDriver<WindowsElement> driver, string xPath, int timeout = 60)
        {
            var timer = new Stopwatch();
            timer.Start();
            while (true)
            {
                if (timer.Elapsed.TotalSeconds > timeout)
                {
                    throw new Exception($"Could not find element {xPath}");
                }
                try
                {
                    driver.FindElementByXPath(xPath);
                    break;
                }
                catch
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            }
            return true;
        }

        protected static bool VerifyPortingResults(string expected, string received)
        {
            var expectedResults = JsonConvert.DeserializeObject<Metric[]>(expected);
            var receivedResults = JsonConvert.DeserializeObject<Metric[]>(received);

            int expectedHash = 0;
            foreach (var er in expectedResults)
            {
                expectedHash += er.ToString().GetHashCode();
            }
            int receivedHash = 0;
            foreach( var rr in receivedResults)
            {
                receivedHash += rr.ToString().GetHashCode();
            }

            return receivedHash == expectedHash;
        }
    }
}