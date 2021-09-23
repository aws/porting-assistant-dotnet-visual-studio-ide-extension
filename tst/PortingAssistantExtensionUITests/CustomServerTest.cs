using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using OpenQA.Selenium;
using System;
using System.Threading;

namespace PortingAssistantExtensionUITests
{
    [TestClass]
    public class CustomServerTest : VisualStudioSession 
    {
        private const string portingResultsFile = @"C:\ide-ui-test-solutions\CustomServer\PortSolutionResult.json";
        private const string solutionFile = @"C:\ide-ui-test-solutions\CustomServer\CustomServer.sln";

        [TestMethod]
        public void RunTest()
        {
            GoToFile("startup.cs");
            StartFullSolutionAssessment();
            WaitForElement("//Pane[starts-with(@Name,\"Assessment successful. You can view the assessment results in th\")]");
            WaitForElement("//DataItem[@ClassName=\"ListViewItem\"][starts-with(@Name,\"PA0002. Add a reference to Microsoft.AspNetCore.Owin and remove \")]");
            PortSolution(true);
            VerifyPortingResults(ExpectedValues.MvcMusicStorePortSolution, File.ReadAllText(portingResultsFile));
        }

        [TestInitialize]
        public void ClassInitialize()
        {
            Setup(solutionFile);
        }

        [TestCleanup]
        public void ClassCleanup()
        {
            TearDown();
        }
    }
}
