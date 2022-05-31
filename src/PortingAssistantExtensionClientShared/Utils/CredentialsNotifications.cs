using EnvDTE;
using Microsoft;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using PortingAssistantVSExtensionClient.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace PortingAssistantExtensionClientShared.Utils
{
    internal class CredentialsNotifications : IVsInfoBarUIEvents
    {
        private IVsShell shell;
        private IVsInfoBarHost host;
        private uint _cookie;
        public async System.Threading.Tasks.Task ShowCredentialsInfoBarAsync(Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider, string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            shell = await ServiceProvider.GetServiceAsync(typeof(SVsShell)) as IVsShell;
            if (shell != null)
            {
                shell.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out var obj);
                host = (IVsInfoBarHost)obj;

                if (host != null)
                {
                    var actions = new List<IVsInfoBarActionItem>
                    {
                        new InfoBarHyperlink("Dismiss", "dismiss"),
                    };
                    InfoBarModel infoBarModel = new InfoBarModel(message, actions, KnownMonikers.StatusInformation, isCloseButtonVisible: false);
                    var factory = await ServiceProvider.GetServiceAsync(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;
                    Assumes.Present(factory);
                    IVsInfoBarUIElement element = factory.CreateInfoBar(infoBarModel);
                    element.Advise(this, out _cookie);
                    host.AddInfoBar(element);
                    AwsUtils.IsCredsNotificationDismissed = false;
                }
            }
        }

        public void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem)
        {
            host.RemoveInfoBar(infoBarUIElement);
            infoBarUIElement.Unadvise(_cookie);
            infoBarUIElement.Close();

            AwsUtils.IsCredsNotificationDismissed = true;
        }

        void IVsInfoBarUIEvents.OnClosed(IVsInfoBarUIElement infoBarUIElement)
        {
        }
    }
}
