using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace PortingAssistantVSExtensionClient.Utils
{
   public static  class CommandUtils
    {
        public static readonly Guid CommandSet = new Guid("72f43848-037a-4907-98e2-e7e964271f44");

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



    }
}
