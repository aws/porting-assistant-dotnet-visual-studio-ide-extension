using Newtonsoft.Json.Linq;
using PortingAssistantVSExtensionClient.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistantVSExtensionClient.Utils
{
    public class FilesUtils
    {
        public static string GetTmpFolder()
        {
            var tmpFolder = Path.Combine(Path.GetTempPath(), "PortingAssistant", Path.GetRandomFileName());
            if (!Directory.Exists(tmpFolder)) Directory.CreateDirectory(tmpFolder);
            return tmpFolder;
        }

        public static void GetInitJsonReady(string initJsonPath, string profileName, string uniqueBucketName)
        {
            dynamic initJson = JObject.Parse(File.ReadAllText(initJsonPath));
            initJson.awsProfile = profileName;
            initJson.s3Bucket = uniqueBucketName;
            initJson.disableTermNiceties = true;
            File.WriteAllText(initJsonPath, initJson.ToString());
        }

        public static void GetDeploymentJsonReady(string tmpPath, DeploymentParameters parameters, string buildpath)
        {
            var AssemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var ConfigurationFileName = Environment.GetEnvironmentVariable("DeploymentConfiguration") ?? Common.Constants.DefaultDeploymentConfiguration;
            var ConfigurationPath = Path.Combine(
                AssemblyPath,
                Common.Constants.ResourceFolder,
                ConfigurationFileName);

            dynamic configuration = JObject.Parse(File.ReadAllText(ConfigurationPath));
            dynamic deploymentconfig = JObject.Parse(File.ReadAllText(tmpPath));

            // configure the depolyment json from the inputs

            deploymentconfig.applicationName = parameters.deployname;
            deploymentconfig.deploymentSource = "BUILD";

            deploymentconfig.exposedPorts = configuration.exposedports;

            deploymentconfig.buildDefinitions.buildParameters.sourceType = "NETCORE";
            deploymentconfig.buildDefinitions.buildParameters.buildLocation = buildpath;

            deploymentconfig.ecsParameters.cpu = configuration.cpu;
            deploymentconfig.ecsParameters.memory = configuration.memory;
            deploymentconfig.ecsParameters.enableCloudwatchLogging = true;
            deploymentconfig.ecsParameters.reuseResources.vpcId = parameters.vpcId;
            deploymentconfig.eksParameters.reuseResources.vpcId = parameters.vpcId;

            File.WriteAllText(tmpPath, deploymentconfig.ToString());
        }
    }
}
