using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using PortingAssistantVSExtensionClient.Common;
using PortingAssistantVSExtensionClient.Dialogs;
using PortingAssistantVSExtensionClient.Models;
using PortingAssistantVSExtensionClient.Options;
using PortingAssistantVSExtensionClient.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using Task = System.Threading.Tasks.Task;

namespace PortingAssistantVSExtensionClient.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class SolutionPortingCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = PackageIds.cmdidSolutionPortingCommand;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid(PackageGuids.guidPortingAssistantVSExtensionClientPackageCmdSetString);

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;


        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionPortingCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private SolutionPortingCommand(AsyncPackage package, OleMenuCommandService commandService)
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
        public static SolutionPortingCommand Instance
        {
            get;
            private set;
        }

        private string solutionName = "";
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
            // Switch to the main thread - the call to AddCommand in SolutionPortingCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new SolutionPortingCommand(package, commandService);
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
            try
            {
                if (!await CommandsCommon.CheckLanguageServerStatusAsync()) return;
                if (!CommandsCommon.SetupPage()) return;
                if (!UserSettings.Instance.SolutionAssessed)
                {
                    NotificationUtils.ShowInfoMessageBox(this.package, "Please run a full assessment before porting", "");
                    return;
                }
                if (UserSettings.Instance.TargetFramework.Equals(TargetFrameworkType.NO_SELECTION))
                {
                    if (!SelectTargetDialog.EnsureExecute()) return;
                }
                string SolutionFile = await CommandsCommon.GetSolutionPathAsync();
                solutionName = Path.GetFileName(SolutionFile);
                var ProjectFiles = SolutionUtils.GetProjectPath(SolutionFile);
                if (!PortingDialog.EnsureExecute(solutionName)) return;
                CommandsCommon.EnableAllCommand(false);
                string pipeName = Guid.NewGuid().ToString();
                CommandsCommon.RunPortingAsync(SolutionFile, ProjectFiles, pipeName, solutionName);
                PipeUtils.StartListenerConnection(pipeName, GetSolutionCompletionTasks(this.package, solutionName, UserSettings.Instance.TargetFramework));
            }
            catch (Exception ex)
            {
                NotificationUtils.ShowErrorMessageBox(this.package, $"Porting failed for {solutionName} due to {ex.Message}", "Porting failed");
                CommandsCommon.EnableAllCommand(true);
            }
        }

        public Func<Task> GetSolutionCompletionTasks(AsyncPackage package, string solutionName, string targetFramework)
        {
            async Task CompletionTask()
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                try
                {
                    var successfulMessage = $"The solution has been ported to {targetFramework}" + (UserSettings.Instance.ApplyPortAction ? $".{Environment.NewLine}Code changes have been applied" : "");
                    NotificationUtils.ShowInfoMessageBox(package, successfulMessage, "Porting successful");
                    await NotificationUtils.ShowInfoBarAsync(package, successfulMessage);
                    await NotificationUtils.UseStatusBarProgressAsync(2, 2, successfulMessage);
                }
                catch (Exception ex)
                {
                    NotificationUtils.ShowErrorMessageBox(package, $"Porting failed for {solutionName} due to {ex.Message}", "Porting failed");
                }
                finally
                {
                    CommandsCommon.EnableAllCommand(true);
                }
            }
            return CompletionTask;
        }

        
    }
}
