﻿using Xunit;
using System;
using Xunit.Abstractions;
using IDE_UITest.UI;

namespace IDE_UITest.Tests.Vs2019
{
    [Collection("Collection2")]
    public class AssessWorkflowTestsVs2019 : TestBase, IDisposable
    {

        private GlobalFixture _fixture;
        private readonly ITestOutputHelper output;
        private VSMainView root;
        public AssessWorkflowTestsVs2019(GlobalFixture fixture, ITestOutputHelper output) :  base(output)
        {
            VSProcessID = 0;
            this.output = output;
            _fixture = fixture;
            var solutionPath = _fixture.InputData["assess-solution-path1"];
            root = LaunchVSWithSolutionWithSecurityWarning(_fixture.Vs2019Location , solutionPath);
            Assert.True(root != null, $"Fail to get visual studio main window after loading solution {solutionPath}");
        }

        [Fact]
        [Trait("Category", "Smoke")]
        public void RunAssessFromAnalyzeMenu()
        {
            root.RunFullAssessFromAnalyzeMenu();
            output.WriteLine("Verify run full assess solution option from Analyze menu");
            root.VerifyEnableIncrementalAssessMenuItemFromAnalyzeMenuExist();
            output.WriteLine("Verify Enable Incremental Assessmen MenuItem from Analyze menu");
        }

        [Fact]
        [Trait("Category", "Smoke")]
        public void RunAssessFromExtensionsMenu()
        {
            root.RunFullAssessFromExtensionsMenu();
            root.VerifyEnableIncrementalAssessMenuItemFromExtensionsMenuExist();
            output.WriteLine("Verify run full assess solution option from Analyze menu");
        }

        [Fact]
        [Trait("Category", "Smoke")]
        public void RunAssessFromSolutionExplorer()
        {
            root.RunFullAssessFromSolutionExplorer();
            root.VerifyEnableIncrementalAssessMenuItemFromSolutionExplorerContextMenu();
            output.WriteLine("Verify run full assess solution option from Analyze menu");
        }
    }
}
