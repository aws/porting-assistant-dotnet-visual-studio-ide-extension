﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using PortingAssistantVSExtensionClient.Common;
using PortingAssistantVSExtensionClient.Dialogs;
using PortingAssistantVSExtensionClient.Options;
using PortingAssistantVSExtensionClient.Utils;

namespace PortingAssistantVSExtensionClient.Commands
{
    public static class CommandsCommon
    {
        public static readonly Guid CommandSet = new Guid(PackageGuids.guidPortingAssistantVSExtensionClientPackageCmdSetString);

        public static void CheckWelcomePage()
        {
            if (UserSettings.Instance.ShowWelcomePage)
            {
                WelcomeDialog welcomeDialog = new WelcomeDialog();
                welcomeDialog.ShowModal();
            }
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
            var serverStatus = await PAGlobalService.Instance.GetLanguageServerStatusAsync();
            if (serverStatus == LanguageServerStatus.NOT_RUNNING)
            {
                NotificationUtils.ShowInfoMessageBox(PAGlobalService.Instance.Package, "Please open a .cs file in the solution", "Porting Assistant is activated.");
                return false;
            }
            else
            {
                await NotificationUtils.LockStatusBarAsync(PAGlobalService.Instance.AsyncServiceProvider, "Porting Assistant is activating.....");
                return true;
            }
        }

        public static async System.Threading.Tasks.Task<string> GetSolutionPathAsync()
        {
            var dte = await PAGlobalService.Instance.GetDTEServiceAsync();
            return await SolutionUtils.GetSolutionPathAsync(dte);
        }
    }
}
