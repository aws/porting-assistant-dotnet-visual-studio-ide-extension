using System;
using Xunit;
using Xunit.Abstractions;
using IDE_UITest.UI;
using IDE_UITest.Helper;

namespace IDE_UITest.Tests.Vs2019
{
    [Collection("Collection1")]
    public class InitialSetUpWorkflowTestsVs2019 : TestBase, IDisposable
    {
        private GlobalFixture _fixture;
        private readonly ITestOutputHelper output;
        private VSMainView root;
        private Secret awsSecret;
        public InitialSetUpWorkflowTestsVs2019(GlobalFixture fixture, ITestOutputHelper output) : base(output)
        {
            VSProcessID = 0;
            this.output = output;
            _fixture = fixture;
            awsSecret = AwsHelper.GetSecret(_fixture.AwsAuth["AwsProfileSecretArn"], _fixture.AwsAuth["AwsRegion"]);
        }

        [Fact]
        [Trait("Category", "Smoke")]
        public void InitialSetUp()
        {
            var paExtensionDownloadUrl = _fixture.InputData["pa-extension-2019-download-url"];
            var paExtensionLocalPath = _fixture.InputData["pa-extension-2019-local-path"];
            var vsixInstallerPath = _fixture.InputData["vsixinstaller-path"];
            DownloadPAExtension(paExtensionDownloadUrl, paExtensionLocalPath);
            output.WriteLine("Download Porting Assistant Extension for Visual Studio 2019");

            InstallPAExtension(vsixInstallerPath, paExtensionLocalPath);
            var vsixInsallerView = GetVsixInstallerWindow();
            vsixInsallerView.InstallVsix();
            output.WriteLine("Install Porting Assistant Extension for Visual Studio 2019");

            var solutionPath = _fixture.InputData["assess-solution-path1"];
            root = LaunchVSWithSolutionWithSecurityWarning(_fixture.Vs2019Location, solutionPath);
            Assert.True(root != null, $"Fail to get visual studio main window after loading solution {solutionPath}");

            var getStartWindow = root.OpenStartUpView();
            getStartWindow.CheckAgreeToShareCheckbox();
            getStartWindow.ClickSaveButtonWithoutProfileSelected();
            getStartWindow.CancelGetStartedWindow(); 
            output.WriteLine("Check Checkbox and profile waring bar in the get start page");

            getStartWindow = root.OpenStartUpView();
            getStartWindow.ClickDefaultProviderChainRadioButton();
            getStartWindow.ClickUseAWSProfileRadioButton();
            output.WriteLine("Check Default Provider Chain Radio Button and AWS Profile Radio Button in the get start page");

            var profileName = "awsuiautomation" + DateTime.Now.ToString("yyyyMMddHHmmssffff");
            getStartWindow.AddAndSelectANewProfile(profileName, awsSecret.test_role_access_key, awsSecret.test_role_secret_key);
            output.WriteLine("Check add a new Profile in the get start page");

            root.SelectTargetFrameworkWhenSetUp();
            output.WriteLine("Check target framework selection in the get start page");

            root.WaitTillAssessmentFinished();
            output.WriteLine("Verify run full assess solution option from Analyze menu");
            root.VerifyEnableIncrementalAssessMenuItemFromAnalyzeMenuExist();
            output.WriteLine("Verify Enable Incremental Assessmen MenuItem from Analyze menu");
        }
    }
}
