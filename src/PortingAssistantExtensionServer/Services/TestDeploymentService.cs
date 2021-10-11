using Microsoft.Extensions.Logging;
using PortingAssistantExtensionServer.Models;
using PortingAssistantExtensionServer.Services;
using PortingAssistantExtensionServer.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace PortingAssistantExtensionServer
{
    internal class TestDeploymentService : BaseService, IDisposable
    {
        private readonly ILogger<TestDeploymentService> _logger;
        public TestDeploymentService(ILogger<TestDeploymentService> logger)
        {
            _logger = logger;
        }

        public int Excute(TestDeploymentRequest request)
        {
            _logger.LogInformation($"start excuting ${request} .....");
            //CreateClientConnectionAsync(request.PipeName);
            var exitcode = RemoteCallUtils.Excute(request.fileName, request.arguments, OutputDataHandler);
            _logger.LogInformation($"finish excuting ${request} with exit code {exitcode}");

            return exitcode;
        }

        private void OutputDataHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                _logger.LogInformation(outLine.Data);
            }
        }

        public void Dispose()
        {
        }
    }
}
