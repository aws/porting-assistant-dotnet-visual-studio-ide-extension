using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistantVSExtensionClient.Utils
{
    public static class RemoteCallUtils
    {
        // startinfo
        /*         ProcessStartInfo(
        RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            FileName = filename,
            Arguments = args
        ) */


        private static ProcessStartInfo GetProcessInfo(String FileName, List<String> args, Boolean UseShellExecute = false, Boolean RedirectStandardOutput = true, Boolean RedirectStandardError = true)
        {
            return new ProcessStartInfo()
            {
                FileName = FileName,
                Arguments = String.Join(" ", args),
                RedirectStandardError = RedirectStandardError,
                RedirectStandardOutput = RedirectStandardOutput,
                UseShellExecute = UseShellExecute,
            };
        }
    }
}
