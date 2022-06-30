using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using PortingAssistantVSExtensionClient.Common;
using PortingAssistantVSExtensionClient.Dialogs;
using PortingAssistantVSExtensionClient.Models;
using PortingAssistantVSExtensionClient.Options;
using PortingAssistantVSExtensionClient.Utils;

namespace PortingAssistantVSExtensionClient.Commands
{
    public static class CommandsCommon
    {
        public static readonly Guid CommandSet = new Guid(PackageGuids.guidPortingAssistantVSExtensionClientPackageCmdSetString);

        public static bool SetupPage()
        {
            if (UserSettings.Instance.ShowWelcomePage)
            {
                return WelcomeDialog.EnsureExecute();
            }
            else return true;
        }

        public static readonly List<int> CommandIDs = new List<int>
        {
            PackageIds.cmdidAutoAssessmentCommand,
            PackageIds.cmdidProjectPortingCommand,
            PackageIds.cmdidSolutionPortingCommand,
            PackageIds.SolutionAssessmentCommandId
        };

        public static bool EnableCommand(int cmdID, bool enableCmd)
        {
            bool cmdUpdated = false;
            var mcs = PAGlobalService.Instance.Package.GetService<IMenuCommandService, OleMenuCommandService>();
            var newCmdID = new CommandID(CommandSet, cmdID);
            MenuCommand mc = mcs.FindCommand(newCmdID);
            if (mc != null)
            {
                mc.Enabled = enableCmd;
                cmdUpdated = true;
            }
            return cmdUpdated;
        }

        public static bool EnableAllCommand(bool enableCmd)
        {
            bool cmdUpdated = false;
            var mcs = PAGlobalService.Instance.Package.GetService<IMenuCommandService, OleMenuCommandService>();
            foreach (int commandId in CommandIDs)
            {
                var newCmdID = new CommandID(CommandSet, commandId);
                MenuCommand mc = mcs.FindCommand(newCmdID);
                if (mc != null)
                {
                    mc.Enabled = enableCmd;
                    cmdUpdated = true;
                }
            }
            return cmdUpdated;
        }

        public static async System.Threading.Tasks.Task<bool> CheckLanguageServerStatusAsync()
        {
            try
            {
                EnableAllCommand(false);
                await NotificationUtils.UseStatusBarProgressAsync(1, 2, "Check Porting Assistant Status.....");
                var serverStatus = await UserSettings.Instance.GetLanguageServerStatusAsync();
                await NotificationUtils.UseStatusBarProgressAsync(2, 2, "");
                int retryInterval = 3000;
                for (int retry = 0; retry < 3 ; retry++)
                {

                    if (serverStatus == LanguageServerStatus.NOT_RUNNING)
                    {
                        System.Threading.Thread.Sleep(retryInterval);
                    }
                    else return true;
                }
                return false;

            }catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            finally
            {
                EnableAllCommand(true);
            }
            
        }

        public static async System.Threading.Tasks.Task<string> GetSolutionPathAsync()
        {
            var dte = await PAGlobalService.Instance.GetDTEServiceAsync();
            return await SolutionUtils.GetSolutionPathAsync(dte);
        }
        public static async System.Threading.Tasks.Task<Dictionary<string, List<string>>> GetMetaReferencesAsync()
        {
            var dte = await PAGlobalService.Instance.GetDTEServiceAsync();
            return await SolutionUtils.GetMetadataReferencesAsync(dte);
        }
        
        public static async System.Threading.Tasks.Task RunAssessmentAsync(string SolutionFile, string pipeName)
        {
            var metaReferences = await CommandsCommon.GetMetaReferencesAsync();
            var analyzeSolutionRequest = new AnalyzeSolutionRequest()
            {
                solutionFilePath = SolutionFile,
                metaReferences = metaReferences,
                PipeName = pipeName,
                settings = new AnalyzerSettings()
                {
                    TargetFramework = UserSettings.Instance.TargetFramework.ToString(),
                    IgnoreProjects = new List<string>(),
                },
            };
            await NotificationUtils.UseStatusBarProgressAsync(1, 2, "Porting Assistant is assessing the solution");
            
            var analyzeSolutionResponse = await PortingAssistantLanguageClient.Instance.PortingAssistantRpc.InvokeWithParameterObjectAsync<AnalyzeSolutionResponse>(
                "analyzeSolution",
                analyzeSolutionRequest);

            UserSettings.Instance.SolutionHasWebFormsProject = analyzeSolutionResponse.hasWebFormsProject;
        }

        public static async System.Threading.Tasks.Task RunPortingAsync(string SolutionFile, List<string> ProjectFiles, string pipeName, string portingFile)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var PortingRequest = new ProjectFilePortingRequest()
            {
                SolutionPath = SolutionFile,
                ProjectPaths = ProjectFiles,
                TargetFramework = UserSettings.Instance.TargetFramework.ToString(),
                IncludeCodeFix = UserSettings.Instance.ApplyPortAction,
                PipeName = pipeName
            };
            try
            {
                await NotificationUtils.UseStatusBarProgressAsync(1, 2, $"Porting {portingFile} in process");
                PortingAssistantLanguageClient.Instance.PortingAssistantRpc.InvokeWithParameterObjectAsync<ProjectFilePortingResponse>(
                    "applyPortingProjectFileChanges",
                    PortingRequest);
            }
            catch (Exception ex)
            {
                await NotificationUtils.UseStatusBarProgressAsync(2, 2, $"Porting {portingFile} failed");
                NotificationUtils.ShowErrorMessageBox(PAGlobalService.Instance.Package, $"Porting failed for {portingFile} due to {ex.Message}", "Porting failed");
            }
        }
    }
}
