
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace PortingAssistantVSExtensionClient.Common
{
    class PAInfoBarService : IVsInfoBarUIEvents
    {
        private readonly IServiceProvider _serviceProvider;
        private uint _cookie;

        private PAInfoBarService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public static PAInfoBarService Instance { get; private set; }

        public static void Initialize(IServiceProvider serviceProvider)
        {
            Instance = new PAInfoBarService(serviceProvider);
        }

        public void OnClosed(IVsInfoBarUIElement infoBarUIElement)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            infoBarUIElement.Unadvise(_cookie);
        }

        public void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            System.Diagnostics.Process.Start((string)actionItem.ActionContext);
        }

        public void ShowInfoBar(string message, ImageMoniker status, string url = "")
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var shell = _serviceProvider.GetService(typeof(SVsShell)) as IVsShell;
            if (shell != null)
            {
                shell.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out var obj);
                var host = (IVsInfoBarHost)obj;

                if (host == null)
                {
                    return;
                }

                InfoBarTextSpan[] spans = new InfoBarTextSpan[] { new InfoBarTextSpan(message)};
                InfoBarActionItem[] actions = new InfoBarActionItem[] { new InfoBarHyperlink("view me", url) };

                InfoBarModel infoBarModel = null;
                if (string.IsNullOrEmpty(url))
                {
                    infoBarModel = new InfoBarModel(spans, status, isCloseButtonVisible: true);
                }
                else
                {
                    infoBarModel = new InfoBarModel(spans, actions, status, isCloseButtonVisible: true);
                }
                var factory = _serviceProvider.GetService(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;
                Assumes.Present(factory);
                IVsInfoBarUIElement element = factory.CreateInfoBar(infoBarModel);
                element.Advise(this, out _cookie);
                host.AddInfoBar(element);
            }
        }
    }
}
