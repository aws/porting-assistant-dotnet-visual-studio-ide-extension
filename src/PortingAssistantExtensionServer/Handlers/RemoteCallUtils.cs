using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PortingAssistantExtensionServer
{
    internal interface IRemoteCallUtils
    {
        public int Execute(String FileName, List<String> args, int timeout = 360000);

    }
    internal class RemoteCallUtils : IRemoteCallUtils
    {
        internal readonly ILogger<TestDeploymentService> _logger;
        public RemoteCallUtils(ILogger<TestDeploymentService> logger)
        {
            _logger = logger;
        }
        public int Execute(String FileName, List<String> args, int timeout = 360000)
        {
            var startInfo = GetProcessInfo(FileName, args);
            try
            {
                using (Process exeProcess = Process.Start(startInfo))
                {
                    exeProcess.OutputDataReceived += OutputDataHandler;
                    exeProcess.ErrorDataReceived += ErrorOutputHandler;
                    exeProcess.BeginOutputReadLine();
                    exeProcess.BeginErrorReadLine();
                    exeProcess.WaitForExit(timeout);
                    return exeProcess.ExitCode;
                }
            }
            catch
            {
                return 1;
            }
        }

        private ProcessStartInfo GetProcessInfo(String FileName, List<String> args, Boolean UseShellExecute = false, Boolean RedirectStandardOutput = true, Boolean RedirectStandardError = true)
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

        private void OutputDataHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                _logger.LogInformation(outLine.Data);
            }
        }

        private void ErrorOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                _logger.LogError(outLine.Data);
            }
        }

    }
}
