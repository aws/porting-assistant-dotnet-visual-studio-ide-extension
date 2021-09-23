using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using OpenQA.Selenium;
using System;
using System.Threading;

namespace PortingAssistantExtensionUITests
{
    [TestClass]
    public class MvcMusicStoreTest : VisualStudioSession 
    {
        public void RunTest()
        {
            GoToFile("AccountController.cs");
            StartFullSolutionAssessment();
            WaitForElement("//Pane[starts-with(@Name,\"Assessment successful. You can view the assessment results in th\")]");
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
    }
}
