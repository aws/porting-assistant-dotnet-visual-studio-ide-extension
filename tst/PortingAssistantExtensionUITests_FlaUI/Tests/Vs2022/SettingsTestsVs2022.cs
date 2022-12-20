using System;
using IDE_UITest.Helper;
using Xunit.Abstractions;
using Xunit;

namespace IDE_UITest.Tests.Vs2022
{
    [Collection("Collection1")]
    public class SettingsTestsVs2022 : TestBase, IDisposable
    {
        private GlobalFixture _fixture;
        private readonly ITestOutputHelper output;
        public SettingsTestsVs2022(GlobalFixture fixture, ITestOutputHelper output) : base(output)
        {
            VSProcessID = 0;
            _fixture = fixture;
            this.output = output;
        }

        [Fact]
        public void CheckSwitchingTargetFramework()
        {
            var root = LaunchVSWithoutCode(_fixture.Vs2022Location);
            var optionsWin = root.OpenSettingsOption();
            optionsWin.VerifyExpectedTargetFrameworkOption(Constants.Version.VS2022);
            output.WriteLine("Verify expected Target Framework selection options");
            optionsWin.SelectTargetFrameWork(".NET 6 (Microsoft LTS)");
            optionsWin.SelectTargetFrameWork(".NET 7 (Standard Term Support)");
        }
    }
}