using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using IDE_UITest.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace IDE_UITest
{

    public class TestBase: IDisposable
    {
        public const string VS2019ExePath = @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\Common7\IDE\devenv.exe";
        const string VS2019Name = "WorkflowHostView";
        public int VS2019ProcessID;
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

        public VSMainView LaunchVSWithoutCode()
        {
            AutomationElement loadWindow;
            LaunchingVS(out loadWindow);
            Assert.Equal(VS2019Name, loadWindow.AutomationId);
            output.WriteLine("find main window");
            GetToCodeWorkflowView userControl = GetToCodeWorkflowView(loadWindow);
            userControl.InvokeContinueWithoutCodeBtn();
            return GetVSMainView();
        }

        public VSMainView LaunchVSWithSolution(string solutionPath)
        {
            AutomationElement loadWindow;
            LaunchingVS(out loadWindow);
            Assert.Equal(VS2019Name, loadWindow.AutomationId);
            output.WriteLine("find main window");
            GetToCodeWorkflowView userControl = GetToCodeWorkflowView(loadWindow);
            userControl.LoadSolution(solutionPath);
            var vsMainView = GetVSMainView(solutionPath);
            vsMainView.WaitTillSolutionLoaded();
            return vsMainView;
        }

        private GetToCodeWorkflowView GetToCodeWorkflowView(AutomationElement loadWindow)
        {
            return WaitForElement(() => loadWindow.FindFirstChild(e => e.ByClassName("GetToCodeWorkflowView"))).
                AsWindow().As<GetToCodeWorkflowView>();
        }

        private VSMainView GetVSMainView(string solutionPath = "")
        {
            var name = "Microsoft Visual Studio";
            if (!string.IsNullOrEmpty(solutionPath)) 
            {
                name = $"{Path.GetFileNameWithoutExtension(solutionPath)} - Microsoft Visual Studio";
            }
            
            return WaitForElement(() => Desktop.FindFirstChild(e => e.ByAutomationId("VisualStudioMainWindow").
                And(e.ByName(name))), 120).AsWindow().As<VSMainView>();
            
        }

        

        private void LaunchingVS(out AutomationElement loadWindow)
        {
            var app = FlaUI.Core.Application.Launch(VS2019ExePath);
            VS2019ProcessID = app.ProcessId;
            output.WriteLine($"Visual Studio 2019 process id is [{VS2019ProcessID}]");

            loadWindow = WaitForElement(() =>
            {
                return Desktop.FindFirstChild(e => e.ByAutomationId(VS2019Name));
            }, 30);
        }

        public void Dispose()
        {
            if (VS2019ProcessID == 0) return;
            Process vs2019Process = Process.GetProcessById(VS2019ProcessID);
            vs2019Process?.Kill(true);

        }
    }

    

}
