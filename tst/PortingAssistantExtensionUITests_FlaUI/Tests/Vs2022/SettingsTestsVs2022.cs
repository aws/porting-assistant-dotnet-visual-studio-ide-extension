using Xunit;
using System;
using System.Diagnostics;
using Xunit.Abstractions;
using IDE_UITest.Helper;

namespace IDE_UITest.Tests.Vs2022
{
    [Collection("Collection1")]
    public class SettingsTestsVs2022 : TestBase, IDisposable
    {
        private GlobalFixture _fixture;
        private readonly ITestOutputHelper output;
        private Helper.Secret awsSecret;
        public SettingsTestsVs2022(GlobalFixture fixture, ITestOutputHelper output) : base(output)
        {
            VSProcessID = 0;
            _fixture = fixture;
            this.output = output;
            awsSecret = AwsHelper.GetSecret(_fixture.AwsAuth["AwsProfileSecretArn"], _fixture.AwsAuth["AwsRegion"]);
        }

        [Fact]
        public void SetupTargetAndNewProfile()
        {
            var root = LaunchVSWithoutCode(_fixture.Vs2022Location);
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
        public void CancelNewProfileCreation()
        {
            var root = LaunchVSWithoutCode(_fixture.Vs2022Location);
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
        public void CheckNewProfileValidation()
        {
            var root = LaunchVSWithoutCode(_fixture.Vs2022Location);
            var optionsWin = root.OpenSettingsOption();
            var profileName = "awsuiautomation" + DateTime.Now.ToString("yyyyMMddHHmmssffff");
            Assert.False(optionsWin.FindAwsProfileByname(profileName), $"Profile selection list should not contain {profileName}");
            optionsWin.CheckNewAwsProfileValidation(profileName, awsSecret.test_role_access_key, awsSecret.test_role_secret_key);
        }

        [Fact]
        public void CheckSwitchingBetweenValidProfile()
        {
            var root = LaunchVSWithoutCode(_fixture.Vs2022Location);
            var optionsWin = root.OpenSettingsOption();
            optionsWin.CheckSwitchAwsProfile();
        }

        [Fact]
        public void CheckSwitchingTargetFramework()
        {
            var root = LaunchVSWithoutCode(_fixture.Vs2022Location);
            var optionsWin = root.OpenSettingsOption();
            optionsWin.VerifyExpectedTargetFrameworkOption(Constants.Version.VS2022);
            output.WriteLine("Verify expected Target Framework selection options");
            optionsWin.SelectTargetFrameWork("netcoreapp3.1");
            optionsWin.SelectTargetFrameWork("net5.0");
        }
    }
}