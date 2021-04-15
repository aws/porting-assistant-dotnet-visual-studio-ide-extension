using Microsoft.VisualStudio.Shell;
using PortingAssistantVSExtensionClient.Common;
using PortingAssistantVSExtensionClient.Dialogs;
using PortingAssistantVSExtensionClient.Models;
using PortingAssistantVSExtensionClient.Options;
using PortingAssistantVSExtensionClient.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Task = System.Threading.Tasks.Task;

namespace PortingAssistantVSExtensionClient.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class SolutionAssessmentCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = PackageIds.SolutionAssessmentCommandId;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid(PackageGuids.guidPortingAssistantVSExtensionClientPackageCmdSetString);

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionAssessmentCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private SolutionAssessmentCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static SolutionAssessmentCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in SolutionAssessmentCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new SolutionAssessmentCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private async void Execute(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            try
            {
                CommandsCommon.CheckWelcomePage();
                CommandsCommon.EnableAllCommand(false);
                if (!await CommandsCommon.CheckLanguageServerStatusAsync()) return;
                string SolutionFile = await CommandsCommon.GetSolutionPathAsync();
                var metaReferences = await CommandsCommon.GetMetaReferencesAsync();

                if (UserSettings.Instance.TargetFramework.Equals(TargetFrameworkType.NO_SELECTION))
                {
                    if (!SelectTargetDialog.EnsureExecute()) return;
                }
                await RunAssessmentAsync(SolutionFile);
            }
            catch (Exception ex)
            {
                await NotificationUtils.ShowInfoBarAsync(ServiceProvider, ex.Message);
            }
            finally
            {
                await NotificationUtils.ReleaseStatusBarAsync(ServiceProvider);
                CommandsCommon.EnableAllCommand(true);
            }
        }

        private async Task RunAssessmentAsync(string SolutionFile)
        {
            var analyzeSolutionRequest = new AnalyzeSolutionRequest()
            {
                solutionFilePath = SolutionFile,
                settings = new AnalyzerSettings()
                {
                    TargetFramework = UserSettings.Instance.TargetFramework.ToString(),
                    IgnoreProjects = new List<string>(),
                },
            };
            await NotificationUtils.LockStatusBarAsync(ServiceProvider, "Porting Assistant is assessing the solution.....");
            await PortingAssistantLanguageClient.Instance.PortingAssistantRpc.InvokeWithParameterObjectAsync<AnalyzeSolutionResponse>(
                "analyzeSolution", 
                analyzeSolutionRequest);
            await NotificationUtils.ShowInfoBarAsync(ServiceProvider, "solution has been assessed successfully!");
            UserSettings.Instance.EnabledContinuousAssessment = true;
            UserSettings.Instance.SaveAllSettings();
        }
    }
}
