using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace PortingAssistantVSExtensionClient.Utils
{
    public class KerborosSideCarUtils
    {
        internal const string s3RootUrl = "https://s3.us-west-2.amazonaws.com/aws.portingassistant.dotnet.datastore/";
        internal const string s3KerberosSideCarTemplate = s3RootUrl+"recommendationsync/Templates/kerberosSideCar";
        internal const string SystemWebElement = "system.web";
        internal const string SystemWebServerElement = "system.webServer";
        internal const string WebConfig = "web.config";
        internal const string EnabledElement = "enabled";
        internal const string WindowsAuthenticationType = "Windows";
        internal const string ModeAttribute = "mode";
        internal const string WindowsAuthSystemWebPath = SystemWebElement+"/authentication";
        internal const string WindowsAuthSystemWebServerPath = SystemWebServerElement+ "/security/authentication/windowsAuthentication";

        internal static List<string> KerberosSideCarTemplateFileNames = new List<string>()
        {
            "Dockerfile",
            "krb5.conf",
            "krb_side_car.py",
            "test_krb_side_car.py"
        };



        public static void AddKerberosTemplatesToProject(DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (Project project in dte.Solution.Projects)
            {
                // only process the folder which has project files
                if (!string.IsNullOrWhiteSpace(project.FileName) && project.FileName.Contains(".csproj"))
                {
                    string projectPath = Path.GetDirectoryName(project.FileName).ToString();
                    System.Configuration.Configuration configXml = LoadWebConfig(projectPath);

                    // if web config exists
                    if (configXml != null)
                    {
                        bool isWindowsAuthenticated = isWindowsAuthEnabled(configXml, SystemWebElement, WindowsAuthSystemWebPath, ModeAttribute, WindowsAuthenticationType) || isWindowsAuthEnabled(configXml, SystemWebServerElement, WindowsAuthSystemWebServerPath, EnabledElement, "true");

                        if (isWindowsAuthenticated)
                        {
                            DownloadTemplateskerberosSideCar(projectPath);
                            // string replace add method
                        }
                    }
                }
            }
               
        }

        private static System.Configuration.Configuration LoadWebConfig(string projectDir)
        {
            string webConfigFile = Path.Combine(projectDir, WebConfig);

            if (File.Exists(webConfigFile))
            {
                try
                {
                    var fileMap = new ExeConfigurationFileMap() { ExeConfigFilename = webConfigFile };
                    var configuration = System.Configuration.ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
                    return configuration;
                }
                catch (Exception ex)
                {
                    // add log exception
                }
            }
            return null;
        }

        private static void DownloadTemplateskerberosSideCar(string projectDir)
        {
            var httpClient = new HttpClient();

            var parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = 2 };
            Parallel.ForEach(KerberosSideCarTemplateFileNames, parallelOptions, fileName =>
            {
                if (!string.IsNullOrEmpty(fileName))
                {
                    var fullFileName = Path.Combine(projectDir, fileName);
                    if (!File.Exists(fullFileName))
                    {
                        var fileContents = httpClient.GetStringAsync(string.Concat(s3KerberosSideCarTemplate, "/", fileName)).Result;
                        File.WriteAllText(fullFileName, fileContents);
                    }
                }
            });
        }

        private static bool isWindowsAuthEnabled(System.Configuration.Configuration configXml, string sectionName, string elementPath, string attributeName, string value)
        {
            ConfigurationSection serverConfig = configXml.Sections[sectionName];
            if (serverConfig == null)
            {
                return false;
            }

            XDocument xDocument = XDocument.Parse(serverConfig.SectionInformation.GetRawXml());
            return ContainsAttributeValue(xDocument, elementPath, attributeName, value);
        }

        private static bool ContainsAttributeValue(XDocument document, string elementPath, string attributeName, string value)
        {
            return document.XPathSelectElements(elementPath).Any(e => e.Attribute(attributeName)?.Value == value);
        }
    }
}
