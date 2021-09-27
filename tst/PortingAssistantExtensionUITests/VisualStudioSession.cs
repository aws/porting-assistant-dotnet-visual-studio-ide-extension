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
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Configuration;


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
            DesiredCapabilities desktopCapabilities = new DesiredCapabilities();
            desktopCapabilities.SetCapability("app", "Root");
            desktopSession = new WindowsDriver<WindowsElement>(new Uri("http://127.0.0.1:4723"), desktopCapabilities);

            if (session == null)
            {
                // Create a new session to launch Notepad application
                DesiredCapabilities appCapabilities = new DesiredCapabilities();
                appCapabilities.SetCapability("app", VSAppId);
                appCapabilities.SetCapability("appArguments", testSolution);
                session = new WindowsDriver<WindowsElement>(new Uri(WindowsApplicationDriverUrl), appCapabilities, TimeSpan.FromMinutes(10));
                Assert.IsNotNull(session);
                Assert.IsNotNull(session.SessionId);

                // Wait out the Visual studio splash screen
                Thread.Sleep(TimeSpan.FromSeconds(30));
                session.LaunchApp();

                session.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);
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
            try
            {
                // Identify the current window handle. You can check through inspect.exe which window this is.
                var currentWindowHandle = session.CurrentWindowHandle;
                // Wait for 5 seconds or however long it is needed for the right window to appear/for the splash screen to be dismissed
                Thread.Sleep(TimeSpan.FromSeconds(45));

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

                SelectTargetFramework();
                GoToFile(".cs");
                ClickPortingAssistantMenuElement("Enable Incremental Assessments with Porting Assistant");
                // enabling incremental assessment should trigger first time setup page
                FirstTimeAwsProfileSetup();
            }
            catch
            {
                // if we are testing in an established environment, just move on.
            }
            firstTimeSetupRequired = false;
        }

        private static void FirstTimeAwsProfileSetup()
        {
            // Look if get started window is presented
            // session.FindElementByXPath("//Window[@ClassName=\"Window\"][@Name=\"Get started\"]");
            var secret = GetSecret();
            session.FindElementByXPath("//Window[@ClassName=\"Window\"][@Name=\"Get started\"]/Button[@ClassName=\"Button\"][@Name=\"Add Named Profile\"]/Text[@ClassName=\"TextBlock\"][@Name=\"Add Named Profile\"]").Click();
            session.FindElementByAccessibilityId("ProfileName").SendKeys("default");
            session.FindElementByAccessibilityId("AccesskeyID").SendKeys(secret.test_role_access_key);
            session.FindElementByAccessibilityId("secretAccessKey").SendKeys(secret.test_role_secret_key);
            session.FindElementByXPath("//Window[@ClassName=\"Window\"][@Name=\"Get started\"]/Window[@ClassName=\"Window\"][@Name=\"Add a Named Profile\"]/Button[@ClassName=\"Button\"][@Name=\"Save Profile\"]").Click();
            Thread.Sleep(TimeSpan.FromSeconds(60));
            session.FindElementByXPath("//Button[@ClassName=\"Button\"][@Name=\" Save \"]").Click();
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

        protected static void ClickPortingAssistantMenuElement(string menuItem)
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
            ClickPortingAssistantMenuElement("Run Full Assessment with Porting Assistant");
        }

        protected static void PortSolution(bool applyFileActions)
        {
            // Menu Action
            ClickPortingAssistantMenuElement("Port Solution to .NET Core with Porting Assistant");

            // Check apply porting actions
            if (applyFileActions)
            {
                session.FindElementByAccessibilityId("ApplyPortActionCheck").Click();
            }
            session.FindElementByXPath("//Button[@ClassName=\"Button\"][@Name=\"Port\"]/Text[@ClassName=\"TextBlock\"][@Name=\"Port\"]").Click();

            // Wait for finish
            WaitForElement("//Window[@ClassName=\"#32770\"][@Name=\"Microsoft Visual Studio\"]/Button[@ClassName=\"Button\"][@Name=\"OK\"]", 180); 

            // Reload projects
            session.FindElementByXPath("//Button[@ClassName=\"Button\"][@Name=\"OK\"]").Click();
            session.FindElementByXPath("//Button[@ClassName=\"Button\"][@Name=\"Reload All\"]").Click();
        }

        protected static void SelectTargetFramework()
        {
            ClickPortingAssistantMenuElement("Settings...");
            session.FindElementByName("General").Click();
            // This not a typo, the automation id is missing the last t
            var frameworksBox = session.FindElementByAccessibilityId("TargeFrameworks");
            frameworksBox.Click();
            frameworksBox.SendKeys(Keys.Down);
            frameworksBox.SendKeys(Keys.Enter);
            session.FindElementByXPath("//Button[@ClassName=\"Button\"][@Name=\"OK\"]").Click();
        }
        protected static void SelectAwsProfile()
        {
            ClickPortingAssistantMenuElement("Settings...");
            session.FindElementByName("Data usage sharing").Click();
            var profilesBox = session.FindElementByAccessibilityId("Profiles");
            profilesBox.Click();
            profilesBox.SendKeys(Keys.Down);
            profilesBox.SendKeys(Keys.Enter);
            session.FindElementByXPath("//Button[@ClassName=\"Button\"][@Name=\"OK\"]").Click();
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

        protected static void SearchErrorList(string input)
        {
            session.FindElementByXPath("//Group[@Name=\"Search Control\"][@AutomationId=\"SearchControl\"]/Edit[@Name=\"Search Error List\"]")
                .SendKeys(input);
        }

        protected static Secret GetSecret()
        {
            string secretName = ConfigurationManager.AppSettings.Get("AwsProfileSecretArn"); 
            string region = ConfigurationManager.AppSettings.Get("AwsRegion");
            string secret = "";

            MemoryStream memoryStream = new MemoryStream();

            IAmazonSecretsManager client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));

            GetSecretValueRequest request = new GetSecretValueRequest();
            request.SecretId = secretName;
            request.VersionStage = "AWSCURRENT"; // VersionStage defaults to AWSCURRENT if unspecified.

            GetSecretValueResponse response = null;

            // In this sample we only handle the specific exceptions for the 'GetSecretValue' API.
            // See https://docs.aws.amazon.com/secretsmanager/latest/apireference/API_GetSecretValue.html
            // We rethrow the exception by default.

             response = client.GetSecretValueAsync(request).Result;

            // Decrypts secret using the associated KMS CMK.
            // Depending on whether the secret is a string or binary, one of these fields will be populated.
            if (response.SecretString != null)
            {
                secret = response.SecretString;
            }
            else
            {
                memoryStream = response.SecretBinary;
                StreamReader reader = new StreamReader(memoryStream);
                string decodedBinarySecret = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(reader.ReadToEnd()));
            }

            var secretObject = JsonConvert.DeserializeObject<Secret>(secret);
            return secretObject;
        }
    }
}