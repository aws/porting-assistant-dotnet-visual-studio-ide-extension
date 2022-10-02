﻿using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using PortingAssistantExtensionClientShared.Models;
using PortingAssistantVSExtensionClient.Common;
using PortingAssistantVSExtensionClient.Dialogs;
using PortingAssistantVSExtensionClient.Models;
using PortingAssistantVSExtensionClient.Options;
using PortingAssistantVSExtensionClient.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.IO.Pipes;
using System.Linq;
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

        private string SolutionName = "";

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
                Workspace workspace = null;
                var componentModel = (IComponentModel)await package.GetServiceAsync(typeof(SComponentModel));
                string serializedWorkspace = null;

                if (componentModel != null)
                {
                    workspace = componentModel.GetService<VisualStudioWorkspace>();
                    serializedWorkspace = ConstructAndSerializeAdhocWorkspace(workspace);
                }

                int allDocuments = workspace.CurrentSolution.Projects.Sum(v => v.Documents.Count());

                var solutionFile = await CommandsCommon.GetSolutionPathAsync();
#if Dev16
                VS19LSPTrigger(solutionFile) ;
#endif
                if (!await CommandsCommon.CheckLanguageServerStatusAsync())
                {
                    NotificationUtils.ShowInfoMessageBox(PAGlobalService.Instance.Package, "Porting Assistant cannot be activated. Please open any .cs/.vb file if its not already opened.", "Porting Assistant can not be activated.");
                    return;
                }
                if (!CommandsCommon.SetupPage()) return;
                // Verify Target framework selection before disabling all commands.
                if (UserSettings.Instance.TargetFramework.Equals(TargetFrameworkType.NO_SELECTION))
                {
                    if (!SelectTargetDialog.EnsureExecute()) return;
                }
                CommandsCommon.EnableAllCommand(false);
                
                SolutionName = Path.GetFileName(solutionFile);
                string pipeName = Guid.NewGuid().ToString();
                // It's intended that we don't await for RunAssessmentAsync function for too long.
                CommandsCommon.RunAssessmentAsync(solutionFile, pipeName, serializedWorkspace);
                PipeUtils.StartListenerConnection(pipeName, GetAssessmentCompletionTasks(this.package, SolutionName));
            }
            catch (Exception ex)
            {
                NotificationUtils.ShowErrorMessageBox(this.package, $"Assessment failed for {SolutionName} due to {ex.Message}", "Assessment failed");
                CommandsCommon.EnableAllCommand(true);
            }
        }

        public Func<Task> GetAssessmentCompletionTasks(AsyncPackage package, string solutionName)
        {
            async Task CompletionTask()
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                try
                {
                    if (!UserSettings.Instance.EnabledContinuousAssessment)
                    {
                        UserSettings.Instance.EnabledContinuousAssessment = true;
                        UserSettings.Instance.UpdateContinuousAssessment();
                        await PortingAssistantLanguageClient.UpdateUserSettingsAsync();
                    }
                    await NotificationUtils.UseStatusBarProgressAsync(2, 2, "Assessment successful");
                    await NotificationUtils.ShowInfoBarAsync(this.package, "Assessment successful. You can view the assessment results in the error list or view the green highlights in your source code.");
                    UserSettings.Instance.SolutionAssessed = true;
                }
                catch (Exception ex)
                {
                    NotificationUtils.ShowErrorMessageBox(package, $"Assessment failed for {solutionName} due to {ex.Message}", "Assessment failed");
                }
                finally
                {
                    CommandsCommon.EnableAllCommand(true);
                }
            }
            return CompletionTask;
        }

        private async void VS19LSPTrigger(string solutionFile)
        {
            try
            {
                var openFile = Path.Combine(Path.GetDirectoryName(solutionFile), "pa.ini");
                if (!File.Exists(openFile))
                {
                    using (File.Create(openFile)) ;
                }
                var dte = await PAGlobalService.Instance.GetDTEServiceAsync();
                dte.ItemOperations.OpenFile(openFile);
                await CommandsCommon.CheckLanguageServerStatusAsync();
            }
            catch (Exception ex)
            {
                NotificationUtils.ShowErrorMessageBox(package, 
                    $"failed to load porting assistant in visual studio 2019 due to {ex.Message}", 
                    "failed to load porting assistant");
            }
            
        }

        public string ConstructAndSerializeAdhocWorkspace(Workspace workspace)
        {
            try
            {
                WorkspaceConfiguration workspaceConfig = new WorkspaceConfiguration
                {
                    workspace = string.Empty,
                    solution = new SolutionConfig
                    {
                        projects = new List<ProjectConfig>()
                    }
                };

                HashSet<string> projectPathList = new HashSet<string>();
                foreach (var curProject in workspace.CurrentSolution.Projects)
                {
                    if (ShouldSkipProject(projectPathList, curProject.FilePath))
                    {
                        continue;
                    }

                    projectPathList.Add(curProject.FilePath);
                    ProjectConfig project = new ProjectConfig
                    {
                        documents = new List<DocumentConfig>()
                    };

                    List<DocumentInfo> docInfoLst = new List<DocumentInfo>();
                    foreach (var doc in curProject.Documents)
                    {
                        var document = new DocumentConfig
                        {
                            documentId = doc.Id.Id.ToString(),
                            assemblyName = curProject.AssemblyName,
                            filePath = doc.FilePath
                        };
                        project.documents.Add(document);
                    }

                    project.projectId = curProject.Id.Id.ToString();
                    project.assemblyName = curProject.AssemblyName;
                    project.language = curProject.Language;
                    project.filePath = curProject.FilePath;
                    project.outputFilePath = curProject.OutputFilePath;
                    project.projectReferences = new List<string>();
                    project.metadataReferencesFilePath = curProject.MetadataReferences.ToList().Select(x => x.Display).ToList();
                    project.analyzerReferences = null;
                    project.parseOptions = null;
                    project.compilationOptions = null;
                    workspaceConfig.solution.projects.Add(project);
                }

                return JsonConvert.SerializeObject(workspaceConfig);
            }
            catch
            {
                // Todo: log exception on IDE client side?
                // For any reason this function failed, by returning null it falls back to default Analyze process using Buildalyzer.
                return null;
            }
        }

        /// <summary>
        /// If a projet is marked to support multiple target frameworks, then VisualStudioWorkspace will 
        /// consider this as two items (different Ids) in workspace.CurrentSolution.Projects.
        /// We don't support this scenario in assessment/port (An Item with same key exception).
        /// Current solution is to use the first one and ignore any later occurrences based on the project file path.
        /// </summary>
        /// <param name="projectPathList"></param>
        /// <param name="currentProjectId"></param>
        /// <returns></returns>
        private bool ShouldSkipProject(HashSet<string> projectPathList, string currentProjectPath)
        {
            return projectPathList.Contains(currentProjectPath);
        }
    }
}
