using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Diagnostics;
using Amazon;
using System.Windows;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using PortingAssistantVSExtensionClient.Models;
using Amazon.S3;
using Amazon.S3.Model;
using System.Windows.Controls;

namespace PortingAssistantVSExtensionClient.Utils
{
    public class SupportedVersionsUtil
    {
        #region Singleton
        private SupportedVersionsUtil()
        {
        }

        public static SupportedVersionsUtil Instance { get { return Nested.instance; } }

        private class Nested
        {
            static Nested() { }

            internal static readonly SupportedVersionsUtil instance = new SupportedVersionsUtil();
        }
        #endregion Singleton

        private SupportedVersionConfiguration _supportedVersionConfiguration;
        public SupportedVersionConfiguration SupportedVersionConfiguration
        {
            get
            {
                if (_supportedVersionConfiguration == null)
                {
                    _supportedVersionConfiguration = SupportedVersionConfiguration.GetDefaultConfiguration();
                }
                return _supportedVersionConfiguration;
            }
        }

        private Version _visualStudioVersion;
        public Version VisualStudioVersion
        {
            get
            {
                if (_visualStudioVersion == null)
                {
                    ThreadHelper.JoinableTaskFactory.Run(async () =>
                    {
                        _visualStudioVersion = await GetVisualStudioVersionAsync();
                    });
                }

                return _visualStudioVersion;
            }
        }


        /// <summary>
        /// This is the official way to get Visual Studio client version.
        /// For more information, visit the VsixCommunity github:
        /// https://github.com/VsixCommunity/Community.VisualStudio.Toolkit/blob/master/demo/VSSDK.TestExtension/ToolWindows/RunnerWindow.cs#L21
        /// </summary>
        /// <returns></returns>
        public async Task<Version> GetVisualStudioVersionAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IVsShell shell = await ServiceProvider.GetGlobalServiceAsync<SVsShell, IVsShell>(swallowExceptions: false);
            shell.GetProperty((int)__VSSPROPID5.VSSPROPID_ReleaseVersion, out object value);

            if (value is string raw)
            {
                return Version.Parse(raw.Split(' ')[0]);
            }

            return null;
        }

        public void UpdateComboBox(ComboBox frameworkComboBox)
        {
            frameworkComboBox.Items.Clear();
            // Sort based on recommended order.
            if (SupportedVersionConfiguration?.Versions != null)
            {
                foreach (var version in SupportedVersionConfiguration.Versions)
                {
                    if (Version.TryParse(version.RequiredVisualStudioVersion, out Version requiredVSVersion) &&
                        requiredVSVersion <= VisualStudioVersion)
                    {
                        frameworkComboBox.Items.Add(version.DisplayName);
                    }
                }
            }
        }
    }
}

