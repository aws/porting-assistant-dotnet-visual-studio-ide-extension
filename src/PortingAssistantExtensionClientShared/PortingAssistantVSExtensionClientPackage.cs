
using Microsoft.VisualStudio.Shell;
using PortingAssistantVSExtensionClient.Common;
using PortingAssistantVSExtensionClient.Options;
using PortingAssistantVSExtensionClient.Utils;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace PortingAssistantVSExtensionClient
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [ProvideAutoLoad(PortingAssistantVSExtensionClientPackage.UIContextGuid, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideUIContextRule(PortingAssistantVSExtensionClientPackage.UIContextGuid,
    name: "Support Csharp and VisualBasic",
    expression: "CSharp | VisualBasic",
    termNames: new[] { "CSharp", "VisualBasic" },
    termValues: new[] { "HierSingleSelectionName:.cs$", "HierSingleSelectionName:.vb$" })]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PortingAssistantVSExtensionClientPackage.PackageGuid)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(GeneralOption),
    Common.Constants.ApplicationName, Common.Constants.OptionName, 0, 0, true)]
    [ProvideOptionPage(typeof(DataSharingOption),
    Common.Constants.ApplicationName, Common.Constants.DataOptionName, 0, 0, true)]
    public sealed class PortingAssistantVSExtensionClientPackage : AsyncPackage
    {
        const string PackageGuid = "89507157-95b2-4fa0-beac-c5d42bdaa734";
        public const string UIContextGuid = "de87fa2f-6efb-4005-9ae1-cf01be4977ae";
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            PAGlobalService.Create(this, this);
            PAInfoBarService.Initialize(this);
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            // We need to notify user if anything happened while trying to retreve supported version from S3.
            // This can only be done after PAGlobalService is created.
            await PortingAssistantLanguageClient.Instance.GetSupportedVersionsAsync();
            await PortingAssistantVSExtensionClient.Commands.SolutionAssessmentCommand.InitializeAsync(this);
            await PortingAssistantVSExtensionClient.Commands.SolutionPortingCommand.InitializeAsync(this);
            await PortingAssistantVSExtensionClient.Commands.ProjectPortingCommand.InitializeAsync(this);
            await PortingAssistantVSExtensionClient.Commands.AutoAssessmentCommand.InitializeAsync(this);
            await PortingAssistantVSExtensionClient.Commands.DisplaySettings.InitializeAsync(this);
            await PortingAssistantVSExtensionClient.Commands.ContactSupportCommand.InitializeAsync(this);
            await PortingAssistantVSExtensionClient.Commands.DocumentCommand.InitializeAsync(this);
            await NotificationUtils.ShowToolRefactoringNotificationAsync(this);
        }
    #endregion
    }
}
