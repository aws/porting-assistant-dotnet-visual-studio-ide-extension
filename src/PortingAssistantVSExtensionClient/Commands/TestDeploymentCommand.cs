using Microsoft.VisualStudio.Imaging;
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
            try
            {
                // await CheckToolExistAsync();

                var tmpFolder = FilesUtils.GetTmpFolder();
                var solutionPath = await CommandsCommon.GetSolutionPathAsync();
                var outputPath = Path.Combine(tmpFolder, "deployment-output.json");

                if (!await CommandsCommon.CheckLanguageServerStatusAsync()) return;
                //if (!CommandsCommon.SetupPage()) return;
                var IsBuildSucceed = await CommandsCommon.IsBuildSucceedAsync();

                if (!IsBuildSucceed)
                {
                    NotificationUtils.ShowInfoBar("Please Build your project before deployment", KnownMonikers.StatusWarning);
                    return;
                }

                // Copy kerberos templates
                //CommandsCommon.AddKerberosTemplatesToProject();



                var parameters = TestDeploymentDialog.GetParameters(solutionPath);
                if (parameters == null) return;

                // init deployment tool
                if (parameters.initDeploymentTool || !CommandsCommon.IsDeploymentInit())
                {
                    await InitDeploymentToolAsync(parameters.profileName, parameters.enableMetrics, tmpFolder);
                }

                var deployemtJsonPath = await GetDeploymentConfigurationPathAsync(tmpFolder, parameters);

                // deploy
                NotificationUtils.ShowInfoBar($"Start to deploy your project {Path.GetFileNameWithoutExtension(parameters.deployname)} on AWS.", KnownMonikers.StatusInformation);
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
                        deployemtJsonPath,
                        "--output-file",
                        outputPath
                        },
                    });

                // update results
                dynamic result = JObject.Parse(File.ReadAllText(outputPath));
                var applicationName = result.appId;
                var status = result.deploymentStatus;
                var url = result.appEndpoint;
                DeploymentDetail deploymentDetail = new DeploymentDetail
                {
                    Status = status,
                    DeployName = applicationName,
                    CreationTime = DateTime.Now.ToString("MM\\/dd\\/yyyy h\\:mm tt"),
                    DeploymentEndpoint = new Uri($"http://{url}")
                };

                CommandsCommon.UpdateDeploymentResults(deploymentDetail);
                // TODO add result management

                if (response.status == 0 && status == "SUCCESS")
                {

                    NotificationUtils.ShowInfoBar($"Succeed running your project on AWS, please view the endpoint: http://{url}", KnownMonikers.StatusInformation, $"http://{url}");
                }
                else
                {
                    NotificationUtils.ShowInfoBar("Failed to deploy on AWS, Please Check the logs for finding the root cuase", KnownMonikers.StatusError);
                }
            }
            catch (Exception ex)
            {
                NotificationUtils.ShowInfoBar(ex.Message, KnownMonikers.StatusError);
            }
        }

        private async Task CheckToolExistAsync()
        {
            try
            {
                /*
                var resp = await PortingAssistantLanguageClient.Instance.PortingAssistantRpc
                .InvokeWithParameterObjectAsync<TestDeploymentResponse>("deploySolution",
                new TestDeploymentRequest()
                {
                    fileName = "init",
                    arguments = new List<string>()
                });
                if (resp.status != 0) throw new Exception("Could not found and Install deployment module");

                */
                if (CommandsCommon.GetEulaType() != "AWS")
                {
                    // TODO Pop EULA
                    if (!EULADialog.EnsureExecute()) return;
                    // Update Eula
                    // CommandsCommon.UpdateEula("AWS");
                }
            }
            catch
            {
                throw new Exception("Check tool existence failed");
            }
        }

        private async Task InitDeploymentToolAsync(string profileName, bool enableMetrics, string tmpFolder)
        {
            try
            {
                var initJsonPath = Path.Combine(tmpFolder, "init.json");
                var uniqueBucketName = await AwsUtils.CreateDefaultBucketAsync(profileName, Common.Constants.DefaultDeploymentBucketName);

                await PortingAssistantLanguageClient.Instance.PortingAssistantRpc
                    .InvokeWithParameterObjectAsync<TestDeploymentResponse>("deploySolution",
                    new TestDeploymentRequest()
                    {
                        fileName = Common.Constants.DefaultDeploymentTool,
                        arguments = new List<string> {
                        "init",
                        "--generate-cli-skeleton",
                        "--cli-output-json",
                        initJsonPath,
                        "--advanced"
                        },
                    });

                dynamic initJson = JObject.Parse(File.ReadAllText(initJsonPath));
                initJson.awsProfile = profileName;
                initJson.s3Bucket = uniqueBucketName;
                initJson.disableTermNiceties = true;

                File.WriteAllText(initJsonPath, initJson.ToString());

                var response = await PortingAssistantLanguageClient.Instance.PortingAssistantRpc
                    .InvokeWithParameterObjectAsync<TestDeploymentResponse>("deploySolution",
                    new TestDeploymentRequest()
                    {
                        fileName = Common.Constants.DefaultDeploymentTool,
                        arguments = new List<string> {
                        "init",
                        "--init-json-file",
                        initJsonPath
                        },
                    });

                CommandsCommon.UpdateInitStatus(response.status == 0);
            }
            catch (Exception ex)
            {
                throw new Exception("init deployment tool failed");
            }
        }

        private async Task<string> GetDeploymentConfigurationPathAsync(string tmpFolder, DeploymentParameters parameters)
        {
            try
            {
                var tmpPath = Path.Combine(tmpFolder, "deployment.json");
                await PortingAssistantLanguageClient.Instance.PortingAssistantRpc.InvokeWithParameterObjectAsync<TestDeploymentResponse>("deploySolution",
                    new TestDeploymentRequest()
                    {
                        fileName = Common.Constants.DefaultDeploymentTool,
                        arguments = new List<string> {
                        "generate",
                        "app-deployment",
                        "--generate-cli-skeleton",
                        "--cli-output-json",
                        tmpPath
                        },
                    });

                var AssemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var ConfigurationFileName = Environment.GetEnvironmentVariable("DeploymentConfiguration") ?? Common.Constants.DefaultDeploymentConfiguration;
                var ConfigurationPath = Path.Combine(
                    AssemblyPath,
                    Common.Constants.ResourceFolder,
                    ConfigurationFileName);

                dynamic configuration = JObject.Parse(File.ReadAllText(ConfigurationPath));
                dynamic deploymentconfig = JObject.Parse(File.ReadAllText(tmpPath));

                // configure the depolyment json from the inputs
                var buildpath = await CommandsCommon.GetBuildOutputPathAsync(parameters.selectedProject);

                deploymentconfig.applicationName = parameters.deployname;
                deploymentconfig.deploymentSource = "BUILD";
                deploymentconfig.exposedPorts = configuration.ExposedPorts;

                deploymentconfig.buildDefinitions.buildParameters.sourceType = "NETCORE";
                deploymentconfig.buildDefinitions.buildParameters.buildLocation = buildpath;

                deploymentconfig.ecsParameters.reuseResources.vpcId = parameters.vpcId;
                deploymentconfig.eksParameters.reuseResources.vpcId = parameters.vpcId;

                File.WriteAllText(tmpPath, deploymentconfig.ToString());
                return tmpPath;
            }
            catch
            {
                throw new Exception("Generate Deployement Configuration failed");
            }
        }
    }
}
