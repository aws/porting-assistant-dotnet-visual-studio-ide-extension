using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using IDE_UITest.UI;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace IDE_UITest
{

    public class TestBase: IDisposable
    {
        const string VSName = "WorkflowHostView";
        public int VSProcessID;
        public AutomationElement Desktop;
        public ITestOutputHelper output;

        public TestBase(ITestOutputHelper output) 
        {
            this.output = output;
            var automation = new UIA3Automation();
            Desktop = automation.GetDesktop();
            //Kill all visual studio process
            KillExistingVSProcesses();
        }

        private void KillExistingVSProcesses()
        {
            var vsProcesses = Process.GetProcesses().
                Where(pr => pr.MainWindowTitle == "Microsoft Visual Studio" || pr.MainWindowTitle == "Microsoft Visual Studio 2019"); // Add 2022 . without '.exe'

            foreach (var process in vsProcesses)
            {
                output.WriteLine($"kill vs process [{process.Id} - {process.MainWindowTitle}]");
                process.Kill(true);
            }
        }

        public T WaitForElement<T>(Func<T> getter, int waitTimeout = 5)
        {
            var retry = Retry.WhileNull<T>(
                () => getter(),
                TimeSpan.FromSeconds(waitTimeout));
            Assert.True(retry.Success, $"Failed to get an element {getter} within a wait timeout");

            return retry.Result;
        }

        public VSMainView LaunchVSWithoutCode(string vsLocation)
        {
            AutomationElement loadWindow;
            LaunchingVS(vsLocation, out loadWindow);
            Assert.Equal(VSName, loadWindow.AutomationId);
            output.WriteLine("find main window");
            GetToCodeWorkflowView userControl = GetToCodeWorkflowView(loadWindow);
            userControl.InvokeContinueWithoutCodeBtn();
            return GetVSMainView();
        }

        private void LaunchVSAndLoadSolution(string vsLocation, string solutionPath)
        {
            AutomationElement loadWindow;
            LaunchingVS(vsLocation, out loadWindow);
            Assert.Equal(VSName, loadWindow.AutomationId);
            output.WriteLine("find main window");
            GetToCodeWorkflowView userControl = GetToCodeWorkflowView(loadWindow);
            userControl.LoadSolution(solutionPath);
        }

        public VSMainView LaunchVSWithSolution(string vsLocation, string solutionPath)
        {
            LaunchVSAndLoadSolution(vsLocation, solutionPath);
            var vsMainView = GetVSMainView(solutionPath);
            vsMainView.WaitTillSolutionLoaded();
            return vsMainView;
        }

        public VSMainView LaunchVSWithSolutionWithSecurityWarning(string vsLocation, string solutionPath)
        {
            LaunchVSAndLoadSolution(vsLocation, solutionPath);
            CheckSecurityWarning();
            var vsMainView = GetVSMainView(solutionPath);
            vsMainView.WaitTillSolutionLoaded();
            return vsMainView;
        }

        private void CheckSecurityWarning()
        {
            var vsWindow = Retry.Find(() => Desktop.FindFirstChild(e => e.ByAutomationId("VisualStudioMainWindow").
                          And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Window))),
                          new RetrySettings
                          {
                              Timeout = TimeSpan.FromSeconds(600),
                              Interval = TimeSpan.FromSeconds(1),
                              ThrowOnTimeout = false,
                          }).AsWindow();
            if (vsWindow != null)
            {
                vsWindow.DrawHighlight();
                var securityWarningDialog = WaitForElement(() => vsWindow.FindFirstChild(e => e.ByLocalizedControlType("dialog")
            .And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Window))), 10).AsWindow();
                var askForEveryProject = WaitForElement(() => securityWarningDialog.FindFirstChild(
                    e => e.ByName("Ask me for every project in this solution")
                .And(e.ByControlType(FlaUI.Core.Definitions.ControlType.CheckBox))), 10).AsCheckBox();
                askForEveryProject.DrawHighlight();
                if (askForEveryProject.IsChecked == true)
                {
                    askForEveryProject.Toggle();
                }
                var OKBtn = WaitForElement(() => securityWarningDialog.FindFirstChild(
                    e => e.ByName("OK").And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Button))), 10).AsButton();
                OKBtn.DrawHighlight();
                OKBtn.Invoke();
            }
        }

        private GetToCodeWorkflowView GetToCodeWorkflowView(AutomationElement loadWindow)
        {
            return WaitForElement(() => loadWindow.FindFirstChild(e => e.ByClassName("GetToCodeWorkflowView"))).
                AsWindow().As<GetToCodeWorkflowView>();
        }

        private VSMainView GetVSMainView(string solutionPath = "")
        {
            var name = "Microsoft Visual Studio (Administrator)";
            if (!string.IsNullOrEmpty(solutionPath))
            {
                name = $"{Path.GetFileNameWithoutExtension(solutionPath)} - Microsoft Visual Studio (Administrator)";
            }

            Desktop.WaitUntilEnabled();

            Retry.WhileFalse(() => Desktop?.FindAllChildren().Length > 0, TimeSpan.FromSeconds(2));
            System.Threading.Thread.Sleep(18000);

            var vsMainView = Retry.Find(() => Desktop.FindFirstChild(e => e.ByAutomationId("VisualStudioMainWindow").
                          And(e.ByName(name))),
                          new RetrySettings
                          {
                              Timeout = TimeSpan.FromSeconds(600),
                              Interval = TimeSpan.FromSeconds(1),
                              ThrowOnTimeout = true,
                              TimeoutMessage = "Cannot find main window " + name,
                          }).AsWindow().As<VSMainView>();



            return vsMainView;
        }

        private void LaunchingVS(string vsLocation, out AutomationElement loadWindow)
        {
            var app = FlaUI.Core.Application.Launch(vsLocation);
            VSProcessID = app.ProcessId;
            output.WriteLine($"Visual Studio process id is [{VSProcessID}]");

            loadWindow = WaitForElement(() =>
            {
                return Desktop.FindFirstChild(e => e.ByAutomationId(VSName));
            }, 120);
        }

        public void DownloadPAExtension(string url, string extensionFileLocation)
        {
            WebClient client = new WebClient();
            client.DownloadFile(url, @extensionFileLocation);
        }

        public void InstallPAExtension(string vsixIntallerLocation, string extensionFileLocation)
        {
            Process process = new Process();
            process.StartInfo.FileName = vsixIntallerLocation;
            process.StartInfo.Arguments = extensionFileLocation;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.Verb = "runas";
            process.Start();
        }

        public VsixInstallerView GetVsixInstallerWindow()
        {
            return WaitForElement(() =>
            {
                return Desktop.FindFirstChild(e => e.ByName("VSIX Installer").
                And(e.ByClassName("NavigationWindow")));
            }, 120).AsWindow().As<VsixInstallerView>();
        }

        public void Dispose()
        {
            if (VSProcessID == 0) return;
            Process vsProcess = Process.GetProcessById(VSProcessID);
            vsProcess?.Kill(true);
        }

        public void CopyDirectory(string sourcePath, string destPath)
        {
            string folderName = Path.GetFileName(sourcePath);
            DirectoryInfo directory = Directory.CreateDirectory(Path.Combine(destPath, folderName));
            string[] files = Directory.GetFileSystemEntries(sourcePath);

            foreach (string file in files)
            {
                if (Directory.Exists(file))
                {
                    CopyDirectory(file, directory.FullName);
                }
                else
                {
                    File.Copy(file, Path.Combine(directory.FullName, Path.GetFileName(file)), true);
                }
            }
        }
    }
}
