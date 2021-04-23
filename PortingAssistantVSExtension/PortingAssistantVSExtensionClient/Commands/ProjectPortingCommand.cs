using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using Task = System.Threading.Tasks.Task;
using PortingAssistantVSExtensionClient.Models;
using EnvDTE;
using EnvDTE80;
using PortingAssistantVSExtensionClient.Utils;
using System.Collections.Generic;
using PortingAssistantVSExtensionClient.Options;
using Microsoft.VisualStudio.PlatformUI;
using PortingAssistantVSExtensionClient.Dialogs;
using PortingAssistantVSExtensionClient.Common;
using System.Threading;

namespace PortingAssistantVSExtensionClient.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class ProjectPortingCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = PackageIds.cmdidProjectPortingCommand;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid(PackageGuids.guidPortingAssistantVSExtensionClientPackageCmdSetString);

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        private IVsThreadedWaitDialog4 _dialog;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectPortingCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private ProjectPortingCommand(AsyncPackage package, OleMenuCommandService commandService)
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
        public static ProjectPortingCommand Instance
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
            // Switch to the main thread - the call to AddCommand in ProjectPortingCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new ProjectPortingCommand(package, commandService);
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
                if (!CommandsCommon.SetupPage()) return;
                CommandsCommon.EnableAllCommand(false);
                if (!await CommandsCommon.CheckLanguageServerStatusAsync()) return;
                string SelectedProjectPath = SolutionUtils.GetSelectedProjectPath();
                if (SelectedProjectPath.Equals(""))
                {
                    NotificationUtils.ShowInfoMessageBox(this.package, "Please select or open a project!", "Porting project to dotnet core");
                    return;
                }
                if (UserSettings.Instance.TargetFramework.Equals(TargetFrameworkType.NO_SELECTION))
                {
                    if (!SelectTargetDialog.EnsureExecute()) return;
                }
                if (!PortingDialog.EnsureExecute()) return;
                string SolutionFile = await CommandsCommon.GetSolutionPathAsync();
                if(await RunPortingAsync(SolutionFile, SelectedProjectPath))
                {
                    NotificationUtils.ShowInfoMessageBox(this.package, $"The project has been ported to {UserSettings.Instance.TargetFramework}", "Porting success!");
                }
            }
            catch (Exception ex)
            {
                await NotificationUtils.ShowInfoBarAsync(this.ServiceProvider, ex.Message);
            }
            finally
            {
                CommandsCommon.EnableAllCommand(true);
            }
        }

        private async System.Threading.Tasks.Task<bool> RunPortingAsync(string SolutionFile, string SelectedProjectPath)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var PortingRequest = new ProjectFilePortingRequest()
            {
                SolutionPath = SolutionFile,
                ProjectPaths = new List<string>() { SelectedProjectPath },
                TargetFramework = UserSettings.Instance.TargetFramework.ToString(),
                IncludeCodeFix = UserSettings.Instance.ApplyPortAction,
            };
            _dialog = await NotificationUtils.GetThreadedWaitDialogAsync(ServiceProvider, _dialog);
            using (var ted = (IDisposable)_dialog)
            {
                try {
                    _dialog.StartWaitDialog("Porting Assistant", "Porting the Project........", "", null, "", 1, false, true);
                    await PortingAssistantLanguageClient.Instance.PortingAssistantRpc.InvokeWithParameterObjectAsync<ProjectFilePortingResponse>(
                        "applyPortingProjectFileChanges",
                        PortingRequest);
                    _dialog.UpdateProgress("Porting in process", $"reassessing the solution......", $"reassessing the solution......", 1, 2, true, out _);
                    await CommandsCommon.RunAssessmentAsync(SolutionFile);
                    _dialog.UpdateProgress("Porting in process", $"solution reassessed", $"solution reassessed", 2, 2, true, out _);
                    return true;
                } catch (Exception ex)
                {
                    NotificationUtils.ShowErrorMessageBox(this.package, ex.Message, "Porting failed!");
                    return false;
                }
                finally
                {
                    _dialog.EndWaitDialog();
                }
            }
        }
    }
}
