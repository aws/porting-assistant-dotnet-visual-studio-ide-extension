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
            await NotificationUtils.LockStatusBarAsync(PAGlobalService.Instance.AsyncServiceProvider, "Check Porting Assistant Status.....");
            var serverStatus = await UserSettings.Instance.GetLanguageServerStatusAsync();
            await NotificationUtils.ReleaseStatusBarAsync(PAGlobalService.Instance.AsyncServiceProvider);
            if (serverStatus == LanguageServerStatus.NOT_RUNNING)
            {
                NotificationUtils.ShowInfoMessageBox(PAGlobalService.Instance.Package, "Please open a .cs file in the solution", "Porting Assistant is not activated.");
                return false;
            }
            else
            {
                return true;
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
        
        public static async System.Threading.Tasks.Task RunAssessmentAsync(string SolutionFile)
        {
            var metaReferences = await CommandsCommon.GetMetaReferencesAsync();
            var analyzeSolutionRequest = new AnalyzeSolutionRequest()
            {
                solutionFilePath = SolutionFile,
                metaReferences = metaReferences,
                settings = new AnalyzerSettings()
                {
                    TargetFramework = UserSettings.Instance.TargetFramework.ToString(),
                    IgnoreProjects = new List<string>(),
                },
            };
            await NotificationUtils.LockStatusBarAsync(PAGlobalService.Instance.AsyncServiceProvider, "Porting Assistant is assessing the solution");
            await PortingAssistantLanguageClient.Instance.PortingAssistantRpc.InvokeWithParameterObjectAsync<AnalyzeSolutionResponse>(
                "analyzeSolution",
                analyzeSolutionRequest);
            await NotificationUtils.ShowInfoBarAsync(PAGlobalService.Instance.AsyncServiceProvider, "Assessment Successful");
            await NotificationUtils.LockStatusBarAsync(PAGlobalService.Instance.AsyncServiceProvider, "Assessment Successful");
        }
    }
}
