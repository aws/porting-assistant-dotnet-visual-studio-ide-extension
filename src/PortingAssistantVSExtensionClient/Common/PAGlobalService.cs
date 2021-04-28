using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime.CredentialManagement;
using System.Collections.Generic;
using System;

namespace PortingAssistantVSExtensionClient.Common
{
    public sealed class PAGlobalService
    {
        private static PAGlobalService instance = null;

        public static Lazy<DTE> DTE = new Lazy<DTE>(() => (EnvDTE.DTE)ServiceProvider.GlobalProvider.GetService(typeof(EnvDTE.DTE)));
        public static Lazy<DTE2> DTE2 = new Lazy<DTE2>(() => (EnvDTE80.DTE2)ServiceProvider.GlobalProvider.GetService(typeof(EnvDTE80.DTE2)));

        public readonly AsyncPackage Package;
        public readonly IAsyncServiceProvider AsyncServiceProvider;

        public static PAGlobalService Instance => instance;

        public static void Create(AsyncPackage package, IAsyncServiceProvider asyncServiceProvider)
        {
            if (instance != null) return;
            instance = new PAGlobalService(package, asyncServiceProvider);
        }
        private PAGlobalService(AsyncPackage package, IAsyncServiceProvider asyncServiceProvider)
        {
            this.Package = package;
            this.AsyncServiceProvider = asyncServiceProvider;
        }

        public async Task<DTE2> GetDTEServiceAsync()
        {
            return (DTE2)await AsyncServiceProvider.GetServiceAsync(typeof(DTE));
        }
    }
}
