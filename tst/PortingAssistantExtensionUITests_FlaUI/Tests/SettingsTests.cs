using Xunit;
using System;
using System.Diagnostics;
using Xunit.Abstractions;
using IDE_UITest.Helper;

namespace IDE_UITest
{
    [Collection("Collection1")]
    public class SettingsTests : TestBase, IDisposable
    {
        private GlobalFixture _fixture;
        private readonly ITestOutputHelper output;
        private Helper.Secret awsSecret;
        public SettingsTests(GlobalFixture fixture, ITestOutputHelper output) : base(output)
        {
            VS2019ProcessID = 0;
            _fixture = fixture;
            this.output = output;
            awsSecret = AwsHelper.GetSecret(_fixture.AwsAuth["AwsProfileSecretArn"], _fixture.AwsAuth["AwsRegion"]);
        }

        [Fact]
        public void LaunchVS2019WithoutCode_SetupTargetAndNewProfile()
        {
            var root = LaunchVSWithoutCode();
            //verify optionWindow is launched
            var optionsWin = root.OpenSettingsOption();
            output.WriteLine("Choose Porting Assistant Extension from menu");
            optionsWin.VerifyExpectedTargetFrameworkOption(Constants.Version.VS2019);
            output.WriteLine("Verify expected Target Framework selection options");
            optionsWin.SelectTargetFrameWork("net5.0");
            output.WriteLine("Save Target Framework");

            var profileName = "awsuiautomation" + DateTime.Now.ToString("yyyyMMddHHmmssffff");
            Assert.False(optionsWin.FindAwsProfileByname(profileName), $"Profile selection list should not contain {profileName}");
            optionsWin.CreateNewAwsProfile(profileName, awsSecret.test_role_access_key, awsSecret.test_role_secret_key);
            output.WriteLine("Verify saving and selecting New Profile");
            optionsWin.SaveSettings();
        }

        [Fact]
        public void LaunchVS2019WithoutCode_CancelNewProfileCreation()
        {
            var root = LaunchVSWithoutCode();
            //verify optionWindow is launched
            var optionsWin = root.OpenSettingsOption();
            optionsWin.ClearCache();
            optionsWin.VerifyDocumentHyperlinkAndCancelNewProfileBtn();
            output.WriteLine("Verify Cancel button in New Profile creation window ");
            optionsWin.VerifyToLearnMoreHyperlinkInDataSharing();
            optionsWin.SetForeground();
            optionsWin.CancelOptionsSettings();
            root.VerifySettingsOptionClosed();
            output.WriteLine("Verify Cancel Settings Option window. ");
        }
    }
}