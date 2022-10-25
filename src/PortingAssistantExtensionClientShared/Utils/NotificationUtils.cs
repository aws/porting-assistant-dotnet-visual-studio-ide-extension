using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft;
using Microsoft.VisualStudio.Imaging;
using PortingAssistantVSExtensionClient.Common;
using Microsoft.VisualStudio.Threading;
using EnvDTE;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PortingAssistantVSExtensionClient.Utils
{
    public static class NotificationUtils
    {
        private static object _inProcessIcon = (short)Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_Build;
        public static void ShowInfoMessageBox(IServiceProvider serviceProvider, string message, string title)
        {
            VsShellUtilities.ShowMessageBox(
                serviceProvider,
                message,
                title,
                Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_INFO,
                Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK,
                Microsoft.VisualStudio.Shell.Interop.OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        public static void ShowErrorMessageBox(IServiceProvider serviceProvider, string message, string title)
        {
            VsShellUtilities.ShowMessageBox(
                serviceProvider,
                message,
                title,
                Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_CRITICAL,
                Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK,
                Microsoft.VisualStudio.Shell.Interop.OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        public static async System.Threading.Tasks.Task UseStatusBarProgressAsync(int currentSteps, int numberOfSteps, string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var dte = await PAGlobalService.Instance.AsyncServiceProvider.GetServiceAsync(typeof(DTE)) as DTE;
            Assumes.Present(dte);
            dte.StatusBar.Progress(true, message, currentSteps, numberOfSteps);

            if (currentSteps == numberOfSteps)
            {
                await System.Threading.Tasks.Task.Delay(1000);
                dte.StatusBar.Progress(false);
            }
        }

        public static async Task ShowToolRefactoringNotificationAsync(IAsyncServiceProvider serviceProvider)
        {
            string message = "Check out the new AWS Toolkit for .NET Refactoring Visual Studio extension for complete .NET modernization.";
            string downloadUrl = "https://marketplace.visualstudio.com/items?itemName=AWSTR.refactoringtoolkit2022";
            var actions = new List<IVsInfoBarActionItem>
            {
                new InfoBarHyperlink("Get the new extension", downloadUrl)
            };
            await ShowInfoBarAsync(serviceProvider, message, actions);
        }


        public static async Task ShowInfoBarAsync(IAsyncServiceProvider ServiceProvider,
            string message,
            List<IVsInfoBarActionItem> actions = null)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var shell = await ServiceProvider.GetServiceAsync(typeof(SVsShell)) as IVsShell;
            if (shell != null)
            {
                shell.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out var obj);
                var host = (IVsInfoBarHost)obj;

                if (host != null)
                {
                    var infoBarModel = actions != null
                        ? new InfoBarModel(message,
                            actions,
                            KnownMonikers.StatusInformation,
                            isCloseButtonVisible: true)
                        : new InfoBarModel(message,
                            KnownMonikers.StatusInformation,
                            isCloseButtonVisible: true);
                    var factory = await ServiceProvider.GetServiceAsync(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;
                    Assumes.Present(factory);
                    IVsInfoBarUIElement element = factory.CreateInfoBar(infoBarModel);
                    host.AddInfoBar(element);
                }
            }
        }

        public static async System.Threading.Tasks.Task<IVsThreadedWaitDialog4> GetThreadedWaitDialogAsync(Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider, IVsThreadedWaitDialog4 dialog)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (dialog == null)
            {
                var factory = await ServiceProvider.GetServiceAsync(typeof(SVsThreadedWaitDialogFactory)) as IVsThreadedWaitDialogFactory;
                Assumes.Present(factory);
                dialog = factory.CreateInstance();
            }
            return dialog;
        }
    }
}
