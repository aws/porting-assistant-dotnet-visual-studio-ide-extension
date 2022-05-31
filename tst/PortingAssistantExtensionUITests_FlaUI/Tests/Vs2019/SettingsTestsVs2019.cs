using Xunit;
using System;
using Xunit.Abstractions;
using IDE_UITest.Helper;

namespace IDE_UITest.Tests.Vs2019
{
    [Collection("Collection2")]
    public class SettingsTestsVs2019 : TestBase, IDisposable
    {
        private GlobalFixture _fixture;
        private readonly ITestOutputHelper output;
        private Secret awsSecret;
        public SettingsTestsVs2019(GlobalFixture fixture, ITestOutputHelper output) : base(output)
        {
            VSProcessID = 0;
            _fixture = fixture;
            this.output = output;
            awsSecret = AwsHelper.GetSecret(_fixture.AwsAuth["AwsProfileSecretArn"], _fixture.AwsAuth["AwsRegion"]);
        }

        [Fact]
        [Trait("Category", "Smoke")]
        public void SetupTargetAndNewProfile()
        {
            var root = LaunchVSWithoutCode(_fixture.Vs2019Location);
            var optionsWin = root.OpenSettingsOption();
            output.WriteLine("Choose Porting Assistant Extension from menu");
            optionsWin.SelectTargetFrameWork("net5.0");
            output.WriteLine("Save Target Framework");

            var profileName = "awsuiautomation" + DateTime.Now.ToString("yyyyMMddHHmmssffff");
            Assert.False(optionsWin.FindAwsProfileByname(profileName), $"Profile selection list should not contain {profileName}");
            optionsWin.CreateNewAwsProfile(profileName, awsSecret.test_role_access_key, awsSecret.test_role_secret_key);
            output.WriteLine("Verify saving and selecting New Profile");
            optionsWin.SaveSettings();
        }

        [Fact]
        [Trait("Category", "Smoke")]
        public void CancelNewProfileCreation()
        {
            var root = LaunchVSWithoutCode(_fixture.Vs2019Location);
            var optionsWin = root.OpenSettingsOption();
            optionsWin.ClearCache();
            optionsWin.VerifyDocumentHyperlinkAndCancelNewProfileBtn();
            output.WriteLine("Verify Cancel button in New Profile creation window");
            optionsWin.VerifyToLearnMoreHyperlinkInDataSharing();
            optionsWin.SetForeground();
            optionsWin.CancelOptionsSettings();
            root.VerifySettingsOptionClosed();
            output.WriteLine("Verify Cancel Settings Option window.");
        }

        [Fact]
        [Trait("Category", "Smoke")]
        public void CheckNewProfileValidation()
        {
            var root = LaunchVSWithoutCode(_fixture.Vs2019Location);
            var optionsWin = root.OpenSettingsOption();
            var profileName = "awsuiautomation" + DateTime.Now.ToString("yyyyMMddHHmmssffff");
            Assert.False(optionsWin.FindAwsProfileByname(profileName), $"Profile selection list should not contain {profileName}");
            optionsWin.CheckNewAwsProfileValidation(profileName, awsSecret.test_role_access_key, awsSecret.test_role_secret_key);
            output.WriteLine("Add a Invalid Shorttime Credential and Verify Validation");
        }

        [Fact]
        [Trait("Category", "Smoke")]
        public void CheckSwitchingBetweenValidProfile()
        {
            var root = LaunchVSWithoutCode(_fixture.Vs2019Location);
            var optionsWin = root.OpenSettingsOption();
            optionsWin.CheckSwitchAwsProfile();
            output.WriteLine("Verify Switching Between Valid Profiles");
        }

        [Fact]
        [Trait("Category", "Smoke")]
        public void CheckSwitchingTargetFramework()
        {
            var root = LaunchVSWithoutCode(_fixture.Vs2019Location);
            var optionsWin = root.OpenSettingsOption();
            optionsWin.VerifyExpectedTargetFrameworkOption(Constants.Version.VS2019);
            output.WriteLine("Verify expected Target Framework selection options");
            optionsWin.SelectTargetFrameWork("netcoreapp3.1");
            optionsWin.SelectTargetFrameWork("net5.0");
            output.WriteLine("Verify Switching Between Target Frameworks");
        }
    }
}