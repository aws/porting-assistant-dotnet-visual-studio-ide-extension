using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft;
using Microsoft.VisualStudio.Imaging;

namespace PortingAssistantVSExtensionClient.Utils
{
    public static class NotificationUtils
    {
        public static async System.Threading.Tasks.Task LockStatusBarAsync(Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider, string massage)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            IVsStatusbar StatusBar = (IVsStatusbar)await ServiceProvider.GetServiceAsync(typeof(SVsStatusbar));
            Assumes.Present(StatusBar);
            if (StatusBar.IsFrozen(out int frozen) != 0)
            {
                StatusBar.FreezeOutput(0);
            }
            StatusBar.SetText(massage);
            object icon = (short)Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_Build;
            StatusBar.Animation(1, ref icon);
            StatusBar.FreezeOutput(1);
        }

        public static async System.Threading.Tasks.Task ReleaseStatusBarAsync(Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            IVsStatusbar StatusBar = (IVsStatusbar)await ServiceProvider.GetServiceAsync(typeof(SVsStatusbar));
            Assumes.Present(StatusBar);
            if (StatusBar.IsFrozen(out int frozen) != 0)
            {
                StatusBar.FreezeOutput(0);
            }
            object icon = (short)Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_Build;
            StatusBar.Animation(0, ref icon);
            StatusBar.Clear();
        }

        public static async System.Threading.Tasks.Task ShowInfoBarAsync(Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider, string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var shell = await ServiceProvider.GetServiceAsync(typeof(SVsShell)) as IVsShell;
            if (shell != null)
            {
                shell.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out var obj);
                var host = (IVsInfoBarHost)obj;

                if (host != null)
                {

                    InfoBarModel infoBarModel = new InfoBarModel(message, KnownMonikers.StatusInformation, isCloseButtonVisible: true);
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
