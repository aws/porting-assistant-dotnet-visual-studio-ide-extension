using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime.CredentialManagement;
using System.Collections.Generic;

namespace PortingAssistantVSExtensionClient.Common
{
    public sealed class PAGlobalService
    {
        private static PAGlobalService instance = null;
        private readonly SharedCredentialsFile sharedProfile = new SharedCredentialsFile();

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

        public List<string> ListProfiles()
        {
            return sharedProfile.ListProfileNames();
        }


        public void SaveProfile(string profileName, AwsCredential credential)
        {
            var profile = new CredentialProfile(
                name: profileName,
                profileOptions: new CredentialProfileOptions
                {
                    AccessKey = credential.AwsAccessKeyId,
                    SecretKey = credential.AwsSecretKey
                });
            profile.Region = Amazon.RegionEndpoint.USEast1;
            sharedProfile.RegisterProfile(profile);
        }
    }
}
