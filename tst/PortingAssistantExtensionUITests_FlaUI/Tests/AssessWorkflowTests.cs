using Xunit;
using System;
using System.Diagnostics;
using Xunit.Abstractions;
using IDE_UITest.UI;

namespace IDE_UITest
{
    [Collection("Collection2")]
    public class AssessWorkflowTests : TestBase, IDisposable
    {

        private GlobalFixture _fixture;
        private readonly ITestOutputHelper output;
        private VSMainView root;
        public AssessWorkflowTests(GlobalFixture fixture, ITestOutputHelper output) :  base(output)
        {
            VS2019ProcessID = 0;
            this.output = output;
            _fixture = fixture;
            var solutionPath = _fixture.InputData["assess-solution-path1"];
            root = LaunchVSWithSolution(solutionPath);
            Assert.True(root != null, $"Fail to get visual studio main window after loading solution {solutionPath}");
        }

        [Fact]
        public void RunAssessFromAnalyzeMenu()
        {
            root.RunFullAssessFromAnalyzeMenu();
            output.WriteLine("Verify run full assess solution option from Analyze menu");
            root.VerifyEnableIncrementalAssessMenuItemFromAnalyzeMenuExist();
            output.WriteLine("Verify Enable Incremental Assessmen MenuItem from Analyze menu");
        }

        [Fact]
        public void RunAssessFromExtensionsMenu()
        {
            root.RunFullAssessFromExtensionsMenu();
            root.VerifyEnableIncrementalAssessMenuItemFromExtensionsMenuExist();
            output.WriteLine("Verify run full assess solution option from Analyze menu");
        }

        [Fact]
        public void RunAssessFromSolutionExplorer()
        {
            root.RunFullAssessFromSolutionExplorer();
            root.VerifyEnableIncrementalAssessMenuItemFromSolutionExplorerContextMenu();
            output.WriteLine("Verify run full assess solution option from Analyze menu");
        }
    }
}
