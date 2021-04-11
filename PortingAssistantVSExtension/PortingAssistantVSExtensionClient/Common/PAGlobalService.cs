using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System.Threading;
using System.Threading.Tasks;

namespace PortingAssistantVSExtensionClient.Common
{
    public sealed class PAGlobalService
    {
        private static PAGlobalService instance = null;
        private TaskCompletionSource<LanguageServerStatus> _languageServerStatus;

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
            this._languageServerStatus = new TaskCompletionSource<LanguageServerStatus>();
            this._languageServerStatus.SetResult(LanguageServerStatus.NOT_RUNNING);
        }

        public void SetLanguageServerStatus(LanguageServerStatus status)
        {
            if(status != LanguageServerStatus.NOT_RUNNING && status != LanguageServerStatus.INITIALIZED)
            {
                this._languageServerStatus = new TaskCompletionSource<LanguageServerStatus>();
            }
            else
            {
                this._languageServerStatus.SetResult(status);
            }
        }
        public async Task<LanguageServerStatus> GetLanguageServerStatusAsync()
        {
            return await this._languageServerStatus.Task;
        }

        public async Task<DTE2> GetDTEServiceAsync()
        {
            return (DTE2)await AsyncServiceProvider.GetServiceAsync(typeof(DTE));
        }
    }
}
