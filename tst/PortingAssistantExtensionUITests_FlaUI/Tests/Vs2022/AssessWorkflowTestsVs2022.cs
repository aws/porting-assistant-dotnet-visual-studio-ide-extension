using IDE_UITest.UI;
using System;
using Xunit;
using Xunit.Abstractions;

namespace IDE_UITest.Tests.Vs2022
{
    [Collection("Collection2")]
    public class AssessWorkflowTestsVs2022 : TestBase, IDisposable
    {

        private GlobalFixture _fixture;
        private readonly ITestOutputHelper output;
        private VSMainView root;
        public AssessWorkflowTestsVs2022(GlobalFixture fixture, ITestOutputHelper output) :  base(output)
        {
            VSProcessID = 0;
            this.output = output;
            _fixture = fixture;
            var solutionPath = _fixture.InputData["assess-solution-path1"];
            root = LaunchVSWithSolution(_fixture.Vs2022Location , solutionPath);
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
