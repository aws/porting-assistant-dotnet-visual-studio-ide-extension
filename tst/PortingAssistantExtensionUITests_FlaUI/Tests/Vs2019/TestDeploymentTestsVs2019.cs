using Xunit;
using IDE_UITest.UI;
using Xunit.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace IDE_UITest.Tests.Vs2019
{
    [Collection("Collection2")]
    public class TestDeploymentTestsVs2022 : TestBase, IDisposable
    {
        private GlobalFixture _fixture;
        private readonly ITestOutputHelper output;
        private VSMainView root;
        private string solutionPath;

        public TestDeploymentTestsVs2022(GlobalFixture fixture, ITestOutputHelper output) : base(output)
        {
            VSProcessID = 0;
            this.output = output;
            _fixture = fixture;
            solutionPath = _fixture.InputData["test-deployment-solution-path1"];
            root = LaunchVSWithSolution(_fixture.Vs2019Location, solutionPath);
            Assert.True(root != null, $"Fail to get visual studio main window after loading solution {solutionPath}");
        }

        [Fact]
        public void RunTestDeployment()
        {
            var publishLocation = _fixture.InputData["publish-target-path1"];
            root.PublishProject(publishLocation, solutionPath);
            root.TestProjectOnAWSValidation();
            root.RunTestProjectOnAWS(publishLocation);
            root.WaitForDeploymentFinish();
            root.CheckCurrentlyDeployedApplications();
        }
    }
}
