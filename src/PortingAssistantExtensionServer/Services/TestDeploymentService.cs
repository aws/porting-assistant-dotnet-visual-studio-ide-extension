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
    internal interface ITestDeploymentService
    {
        public int DownloadManifest(string source);

        public int InitDeploymentTool();

        public TestDeploymentResponse Execute(TestDeploymentRequest request);

        public int DownloadAndInstallTool(string source, string toolname);

    }
    internal class TestDeploymentService : BaseService, ITestDeploymentService
    {
        private readonly ILogger<TestDeploymentService> _logger;
        private readonly string tmpFolder;
        private readonly IRemoteCallUtils _remoteCallUtils;

        public TestDeploymentService(ILogger<TestDeploymentService> logger)
        {
            _logger = logger;
            tmpFolder = FileUtils.GetTmpFolder();
            _remoteCallUtils = new RemoteCallUtils(_logger);
        }

        public TestDeploymentService(ILogger<TestDeploymentService> logger, IRemoteCallUtils remoteCallUtils)
        {
            _logger = logger;
            tmpFolder = FileUtils.GetTmpFolder();
            _remoteCallUtils = remoteCallUtils;
        }

        // Upgrade or Download Deployment Tool
        public int InitDeploymentTool()
        {
            try
            {
                var command = Constants.DefaultDeploymentApp;
                var exists = FileUtils.ExistsOnPath(command);
                if (exists)
                {
                    var args = new List<String> { "upgrade" };
                    var exitcode = _remoteCallUtils.Execute(command, args);
                    if (exitcode == 0) _logger.LogInformation("Deployment tool exists and upgrade success");
                    else _logger.LogInformation("Deployment tool exists but upgarde failed");
                    return exitcode;
                }
                else
                {
                    var exitcode = DownloadAndInstallTool(Constants.DefalutDeploymentAppSource, command);
                    if (exitcode == 0) _logger.LogInformation("Deployment tool installation success");
                    return exitcode;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to init deployement tool with error", ex);
                return -1;
            }
        }

        // Download Mainfest file
        public int DownloadManifest(string source)
        {
            try
            {
                var mainfest = Path.Combine(tmpFolder, "mainfest.json");
                FileUtils.Download(source, mainfest);
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to Download mainfest.json file with error : {ex.Message}");
                return -1;
            }

        }

        public TestDeploymentResponse Execute(TestDeploymentRequest request)
        {
            // take a init command from front
            if (request.excutionType == "CheckToolExist")
            {
                var status = InitDeploymentTool();
                return new TestDeploymentResponse
                {
                    status = status
                };
            }

            if (request.excutionType == "CheckManiFest")
            {
                var status = DownloadManifest(Constants.Manifestpath);
                return new TestDeploymentResponse
                {
                    status = status,
                    message = Path.Combine(tmpFolder, "manifest.json")
                };
            }

            _logger.LogInformation($"start excuting ${request.command + String.Join(" ", request.arguments)} .....");
            //CreateClientConnectionAsync(request.PipeName);
            var exitcode = _remoteCallUtils.Execute(request.command, request.arguments);
            _logger.LogInformation($"finish excuting ${request.command + String.Join(" ", request.arguments)} with exit code {exitcode}");

            return new TestDeploymentResponse
            {
                status = exitcode
            };
        }

        public int DownloadAndInstallTool(string source, string toolname)
        {
            try
            {
                var tmpfolder = FileUtils.GetTmpFolder();
                var tmplocation = Path.Combine(tmpfolder, toolname);
                _logger.LogInformation("Starting Downloading Deployment tool");
                FileUtils.Download(source, tmplocation);
                _logger.LogInformation("Finishing Downloading Deployment tool");
                using (ZipArchive archive = ZipFile.Open(tmplocation, ZipArchiveMode.Read))
                {
                    archive.ExtractToDirectory(tmpfolder);
                }
                var installPath = Path.Combine(tmpfolder, Constants.InstallPath);

                var exitcode = _remoteCallUtils.Execute("powershell.exe", new List<string> { installPath, "-acceptEula", "true" });

                // Clean and Delete tmp folder
                Directory.Delete(tmpfolder, true);
                return exitcode;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to install tool with error: {ex.Message} ");
                return -1;
            }
        }
    }

}