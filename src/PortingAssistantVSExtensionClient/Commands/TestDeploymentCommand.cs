using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PortingAssistantVSExtensionClient.Dialogs;
using PortingAssistantVSExtensionClient.Models;
using PortingAssistantVSExtensionClient.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace PortingAssistantVSExtensionClient.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class TestDeploymentCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = PackageIds.cmdidTestDeploymentCommand;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid(PackageGuids.guidPortingAssistantVSExtensionClientPackageCmdSetString);

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestDeploymentCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private TestDeploymentCommand(AsyncPackage package, OleMenuCommandService commandService)
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
        public static TestDeploymentCommand Instance
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
            // Switch to the main thread - the call to AddCommand in TestDeploymentCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new TestDeploymentCommand(package, commandService);
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
            if (!await CommandsCommon.CheckLanguageServerStatusAsync()) return;
            if (!CommandsCommon.SetupPage()) return;
            var IsBuildSucceed = await CommandsCommon.IsBuildSucceedAsync();
            if (!IsBuildSucceed)
            {
                NotificationUtils.ShowErrorMessageBox(package, "failed", "failed");
                return;
            }

            var solutionPath = await CommandsCommon.GetSolutionPathAsync();
            DeploymentParameters parameters = TestDeploymentDialog.GetParameters();
<<<<<<< HEAD
            string buildOutputPath = await CommandsCommon.GetBuildOutputPathAsync(parameters.ProjectPath);

            if(string.IsNullOrWhiteSpace(buildOutputPath))
            {
                //    NotificationUtils.ShowErrorMessageBox(package, "failed", "failed");
                //    return;
            }
            parameters.BuildFolderPath = buildOutputPath;

            await PortingAssistantLanguageClient.Instance.PortingAssistantRpc.InvokeWithParameterObjectAsync<TestDeploymentResponse>(
        "deploySolution",
        new TestDeploymentRequest()
        {
            fileName = solutionPath,
            arguments = new List<string>(),
        });
=======
>>>>>>> fb7c332 (Add AWS utils for create resouce in AWS account.)

            var AssemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var ConfigurationFileName = Environment.GetEnvironmentVariable("DeploymentConfiguration") ?? Common.Constants.DefaultDeploymentConfiguration;
            var ConfigurationPath = Path.Combine(
                AssemblyPath,
                Common.Constants.ResourceFolder,
                ConfigurationFileName);

            var deploymentjson = Path.Combine(AssemblyPath, Common.Constants.ResourceFolder, "deployment.json");

            dynamic configuration = JObject.Parse(File.ReadAllText(ConfigurationPath));
            dynamic deploymentconfig = JObject.Parse(File.ReadAllText(deploymentjson));

            deploymentconfig.applicationName = Path.GetFileName(solutionPath);
            deploymentconfig.buildDefinitions.buildParameters.buildLocation = @"C:\Users\lwwnz\Downloads\AWSApp2Container-installer-windows\deployment.json";

            var tmpPath = Path.Combine(Path.GetTempPath(), "deployment.json");
            File.WriteAllText(tmpPath, deploymentconfig.ToString());

            await initDeploymentToolAsync("test", true);

            var response = await PortingAssistantLanguageClient.Instance.PortingAssistantRpc
                .InvokeWithParameterObjectAsync<TestDeploymentResponse>("deploySolution",
                new TestDeploymentRequest()
                {
                    fileName = Common.Constants.DefaultDeploymentTool,
                    arguments = new List<string> {
                        "generate",
                        "app-deployment",
                        "--deploy",
                        "--input-deployment-files",
                        tmpPath
                        //@"C:\Users\lwwnz\Downloads\AWSApp2Container-installer-windows\deployment.json"
                    },
                });

            if (response.status == 0)
            {
                NotificationUtils.ShowInfoMessageBox(package, "success", "success");
            }
            else
            {
                NotificationUtils.ShowErrorMessageBox(package, "failed", "failed");
            }
        }

        private async Task initDeploymentToolAsync(string profileName, bool enableMetrics)
        {
            await AwsUtils.CreateDefaultBucketAsync(profileName, Common.Constants.DefaultDeploymentBucketName);
            /*
            await PortingAssistantLanguageClient.Instance.PortingAssistantRpc
                .InvokeWithParameterObjectAsync<TestDeploymentResponse>("deploySolution",
                new TestDeploymentRequest()
                {
                    fileName = Common.Constants.DefaultDeploymentTool,
                    arguments = new List<string> {
                        "init",
                        @"C:\Users\lwwnz\Downloads\AWSApp2Container-installer-windows\deployment.json"
                    },
                });
            */
        }
    }
}
