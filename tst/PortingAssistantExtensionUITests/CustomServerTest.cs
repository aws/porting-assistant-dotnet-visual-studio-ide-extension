using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace PortingAssistantExtensionUITests
{
    [TestClass]
    public class CustomServerTest : VisualStudioSession 
    {
        private readonly string portingResultsFile;
        private readonly string solutionFile;

        public CustomServerTest() : base(@"C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\Common7\IDE\devenv.exe")
        {
            portingResultsFile = $"{testSolutionsDir}\\CustomServer\\PortSolutionResult.json";
            solutionFile = $"{testSolutionsDir}\\CustomServer\\CustomServer.sln";
        }

        [TestMethod]
        public void RunTest()
        {
            GoToFile("startup.cs");
            StartFullSolutionAssessment();
            WaitForElement("//Pane[starts-with(@Name,\"Assessment successful. You can view the assessment results in th\")]", 120);
            // Filter error list
            SearchErrorList("PA000");
            WaitForElement("//DataItem[@ClassName=\"ListViewItem\"][starts-with(@Name,\"PA0002. Add a reference to Microsoft.AspNetCore.Owin\")]");
            PortSolution(true);
            VerifyPortingResults(ExpectedValues.MyCustomServer, File.ReadAllText(portingResultsFile));
        }

        [TestInitialize]
        public void ClassInitialize()
        {
            Assert.IsTrue(File.Exists(@"C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\Common7\IDE\devenv.exe"));
            Setup(solutionFile);
        }

        [TestCleanup]
        public void ClassCleanup()
        {
            TearDown();
        }
    }
}
