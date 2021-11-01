using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft;
using Microsoft.VisualStudio.Imaging;
using PortingAssistantVSExtensionClient.Common;
using Microsoft.VisualStudio.Threading;
using EnvDTE;
using Microsoft.VisualStudio.Imaging.Interop;

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


        

        public static void ShowInfoBar( string message, ImageMoniker status, string url = "")
        {
            PAInfoBarService.Instance.ShowInfoBar(message, status, url);
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
