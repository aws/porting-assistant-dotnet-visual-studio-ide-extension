using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using PortingAssistantVSExtensionClient.Commands;

namespace PortingAssistantVSExtensionClient.Utils
{
   public static  class CommandUtils
    {
        public static readonly Guid CommandSet = new Guid(PackageGuids.guidPortingAssistantVSExtensionClientPackageCmdSetString);

        public static readonly List<int> CommandIDs = new List<int>
        {
            PackageIds.cmdidAutoAssessmentCommand,
            PackageIds.cmdidProjectPortingCommand,
            PackageIds.cmdidSolutionPortingCommand,
            PackageIds.SolutionAssessmentCommandId
        };

        public static bool EnableCommand(AsyncPackage package, int cmdID, bool enableCmd)
        {
            bool cmdUpdated = false;
            var mcs = package.GetService<IMenuCommandService, OleMenuCommandService>();
            var newCmdID = new CommandID(CommandSet, cmdID);
            MenuCommand mc = mcs.FindCommand(newCmdID);
            if (mc != null)
            {
                mc.Enabled = enableCmd;
                cmdUpdated = true;
            }
            return cmdUpdated;
        }

        public static bool EnableAllCommand(AsyncPackage package, bool enableCmd)
        {
            bool cmdUpdated = false;
            var mcs = package.GetService<IMenuCommandService, OleMenuCommandService>();
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



    }
}
