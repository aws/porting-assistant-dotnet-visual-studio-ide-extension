using Microsoft.Extensions.Logging;
using PortingAssistantExtensionServer.Models;
using PortingAssistantExtensionServer.Services;
using PortingAssistantExtensionServer.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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

        // init Deployment Infra
        public void init()
        {
            try
            {
                var command = Constants.DefaultDeploymentApp;
                var exists = FileUtils.ExistsOnPath(command);
                if (exists)
                {
                    var args = new List<String> { "upgrade" };
                    var exitcode = RemoteCallUtils.Excute(command, args, OutputDataHandler);
                    if (exitcode == 0) _logger.LogInformation("Deployment tool exists and upgrade success");
                    else _logger.LogInformation("Deployment tool exists but upgarde failed");
                }
                else
                {
                    var tmp = Path.GetTempPath();
                    var tmpfolder = Path.Combine(tmp, Path.GetRandomFileName());
                    if (!Directory.Exists(tmpfolder)) Directory.CreateDirectory(tmpfolder);
                    var tmplocation = Path.Combine(tmpfolder, command);
                    _logger.LogInformation("Starting Downloading Deployment tool");
                    FileUtils.download(Constants.DefalutDeploymentAppSource, tmplocation);
                    _logger.LogInformation("Finishing Downloading Deployment tool");
                    using (ZipArchive archive = ZipFile.Open(tmplocation, ZipArchiveMode.Read))
                    {
                        archive.ExtractToDirectory(tmpfolder);
                    }
                    var installPath = Path.Combine(tmpfolder, Constants.InstallPath);

                    //var installPath = @"C:\Users\lwwnz\Downloads\AWSApp2Container-installer-windows\install.ps1";

                    var exitcode = RemoteCallUtils.Excute("powershell.exe", new List<string> { installPath, "-acceptEula", "true" }, OutputDataHandler);
                    if (exitcode == 0) _logger.LogInformation("Deployment tool installation success");
                    else _logger.LogInformation("Deployment tool installation failed");

                    // Clean and Delete tmp folder
                    Directory.Delete(tmpfolder, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to init deployement tool with error", ex);
            }
        }

        public int Excute(TestDeploymentRequest request)
        {
            // take a init command from front
            if (request.fileName == "init")
            {
                init();
                return 0;
            }

            _logger.LogInformation($"start excuting ${request.fileName + String.Join(" ", request.arguments)} .....");
            //CreateClientConnectionAsync(request.PipeName);
            var exitcode = RemoteCallUtils.Excute(request.fileName, request.arguments, OutputDataHandler);
            _logger.LogInformation($"finish excuting ${request.fileName + String.Join(" ", request.arguments)} with exit code {exitcode}");

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
