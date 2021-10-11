using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PortingAssistantExtensionServer.Utils
{
    public static class RemoteCallUtils
    {
        public static int Excute(String FileName, List<String> args, DataReceivedEventHandler OutputHandler)
        {
            var startInfo = GetProcessInfo(FileName, args);
            try
            {
                using (Process exeProcess = Process.Start(startInfo))
                {
                    exeProcess.OutputDataReceived += OutputHandler;
                    exeProcess.ErrorDataReceived += OutputHandler;
                    exeProcess.BeginOutputReadLine();
                    exeProcess.BeginErrorReadLine();
                    exeProcess.WaitForExit();
                    return exeProcess.ExitCode;
                }
            }
            catch
            {
                return 1;
            }
        }

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
