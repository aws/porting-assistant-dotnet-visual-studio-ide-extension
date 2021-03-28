using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using PortingAssistantVSExtensionClient.Models;
using PortingAssistantVSExtensionClient.Options;
using PortingAssistantVSExtensionClient.Utils;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
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
        public const int CommandId = 0x0101;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("72f43848-037a-4907-98e2-e7e964271f44");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        private IVsThreadedWaitDialog4 _dialog;

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
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var dte = (DTE2)await ServiceProvider.GetServiceAsync(typeof(DTE));
            try
            {
                string SolutionFile = await SolutionUtils.GetSolutionPathAsync(dte);
                var ProjectFiles = SolutionUtils.GetProjectPath(SolutionFile);
                if (UserSettings.Instance.TargetFramework == TargetFrameworkType.no_selection)
                {
                    //selection
                }
                var PortingRequest = new ProjectFilePortingRequest()
                {
                    SolutionPath = SolutionFile,
                    ProjectPaths = ProjectFiles,
                    TargetFramework = UserSettings.Instance.TargetFramework.ToString(),
                    InludeCodeFix = true
                };
                _dialog = await NotificationUtils.GetThreadedWaitDialogAsync(ServiceProvider, _dialog);
                using (var ted = (IDisposable)_dialog)
                {
                    _dialog.StartWaitDialog("Porting Assistant", "Porting the solution........", "", null, "", 1, true, true);
                    CommandUtils.EnableCommand(this.package, CommandId, false);
                    await PortingAssistantLanguageClient.Instance.PortingAssistantRpc.InvokeWithParameterObjectAsync<Models.ProjectFilePortingResponse>("applyPortingProjectFileChanges", new { ProjectPaths = ProjectFiles, SolutionPath = SolutionFile, TargetFramework = "netcoreapp3.1", RecommendedActions = Array.Empty<string>() });
                    _dialog.EndWaitDialog();
                }
                // Show a message box to prove we were here
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    "Porting success!",
                    "",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                // Show a message box to prove we were here
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    "Porting failed!",
                    "",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
            finally
            {
                CommandUtils.EnableCommand(this.package, CommandId, true);
            }
        }
    }
}
