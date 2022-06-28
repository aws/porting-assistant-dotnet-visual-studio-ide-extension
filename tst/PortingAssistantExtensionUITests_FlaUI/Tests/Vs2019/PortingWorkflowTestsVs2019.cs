using Xunit;
using System;
using System.Collections.Generic;
using System.Text;
using IDE_UITest.UI;
using Xunit.Abstractions;
using System.IO;

namespace IDE_UITest.Tests.Vs2019
{
    [Collection("Collection2")]
    public class PortingWorkflowTestsVs2022 : TestBase, IDisposable
    {
        private GlobalFixture _fixture;
        private readonly ITestOutputHelper output;
        private VSMainView root;
        public PortingWorkflowTestsVs2022(GlobalFixture fixture, ITestOutputHelper output) : base(output)
        {
            VSProcessID = 0;
            this.output = output;
            _fixture = fixture;
            var sourcePath = _fixture.InputData["porting-path-source"];
            var destPath = _fixture.InputData["porting-path-destination"];
            var solutionPath = _fixture.InputData["porting-solution-path"];
            CopyDirectory(sourcePath, destPath);
            root = LaunchVSWithSolution(_fixture.Vs2019Location, solutionPath);
            Assert.True(root != null, $"Fail to get visual studio main window after loading solution {solutionPath}");
            root.RunFullAssessFromAnalyzeMenu();
            output.WriteLine("Verify run full assess solution option from Analyze menu");
            root.VerifyEnableIncrementalAssessMenuItemFromAnalyzeMenuExist();
            output.WriteLine("Verify Enable Incremental Assessmen MenuItem from Analyze menu");
        }

        [Fact]
        public void RunSolutionPortingFromExtensionMenu()
        {
            root.RunFullSolutionPortingFromExtensionMenu();
            output.WriteLine("Verify Run Full Solution Porting From Extension Menu");
        }

        [Fact]
        public void RunProjectPortingFromExtensionMenu()
        {
            root.RunSingleProjectPortingFromExtensionMenu();
            output.WriteLine("Verify Run Single Project Porting From Extension Menu");
        }

        [Fact]
        public void RunSolutionPortingFromSolutionExployer()
        {
            root.RunFullSolutionPortingFromSolutionExplorer();
            output.WriteLine("Verify Run Full Solution Porting From Solution Exployer");
        }

        [Fact]
        public void RunProjectPortingFromSolutionExployer()
        {
            root.RunSingleProjectPortingFromSolutionExplorer();
            output.WriteLine("Verify Run Single Project Porting From Solution Exployer");
        }

        [Fact]
        public void RunSolutionPortingFromProjectMenu()
        {
            root.RunFullSolutionPortingFromProjectMenu();
            output.WriteLine("Verify Run Full Solution Porting From Project Menu");
        }

        [Fact]
        public void RunProjectPortingFromProjectMenu()
        {
            root.RunSingleProjectPortingFromProjectMenu();
            output.WriteLine("Verify Run Single Project Porting From Project Menu");
        }
    }
}
