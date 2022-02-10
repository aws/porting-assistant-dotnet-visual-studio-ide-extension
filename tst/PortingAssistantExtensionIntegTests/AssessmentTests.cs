using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace PortingAssistantExtensionIntegTests
{
    class AssessmentTests
    {
        public const string NETCOREAPP31 = "netcoreapp3.1";
        public const string NET50 = "net5.0";
        private string tempProjectRoot;
        private List<string[]> testProjectInfoList = new List<string[]>();
        private string zipRootFolderPath;
        private string testRootPath;
        private string clientConfigPath;
        private Dictionary<string, string[]> testFilesInfo = new Dictionary<string, string[]>()
        {   // Zipfile = {folder-name, solution file, json file}
            ["Miniblog.Core-master.zip"] = new string[] { "Miniblog.Core-master", "Miniblog.Core.sln", "Miniblog.Core.json" },
            ["MvcMusicStore.zip"] = new string[] { "MvcMusicStore", "MvcMusicStore.sln", "MvcMusicStore.json", "MvcMusicStorePort.json" },
            ["NopCommerce-3.1.zip"] = new string[] { "NopCommerce-3.1", "NopCommerce.sln", "NopCommerce.json" },
            ["StarWars.zip"] = new string[] { "StarWars", "StarWars.sln", "StarWars.json" },
            ["Miniblog.Core-master-PortResults.zip"] = new string[] { "Miniblog.Core-master-PortResults", "Miniblog.Core.sln", "Miniblog.Core.json" },
            ["MvcMusicStore-PortResults.zip"] = new string[] { "MvcMusicStore-PortResults", "MvcMusicStore.sln", "MvcMusicStore.json", "MvcMusicStorePort.json" },
            ["MvcMusicStore-WithFix-PortResults.zip"] = new string[] { "MvcMusicStore-WithFix-PortResults", "MvcMusicStore.sln", "MvcMusicStore.json", "MvcMusicStorePort.json" },
            ["MvcMusicStore-net50-PortResults.zip"] = new string[] { "MvcMusicStore-net50-PortResults", "MvcMusicStore.sln", "MvcMusicStore.json", "MvcMusicStorePort.json" },
            ["NopCommerce-3.1-PortResults.zip"] = new string[] { "NopCommerce-3.1-PortResults", "NopCommerce.sln", "NopCommerce.json" },
            ["StarWars-WithFix-PortResults.zip"] = new string[] { "StarWars-WithFix-PortResults", "StarWars.sln", "StarWars.json" },
            ["MvcMusicStore-WithFix-net50-PortResults.zip"] = new string[] { "MvcMusicStore-WithFix-net50-PortResults", "MvcMusicStore.sln", "MvcMusicStore.json" },
            ["Miniblog.Core-master-net50-PortResults.zip"] = new string[] { "Miniblog.Core-master-net50-PortResults", "Miniblog.Core.sln", "Miniblog.Core.json" },
            ["StarWars-WithFix-net50-PortResults.zip"] = new string[] { "StarWars-WithFix-net50-PortResults", "StarWars.sln", "StarWars.json" },
        };

        protected static class TargetFramework
        {
            public const string DotnetCoreApp31 = "netcoreapp3.1";
            public const string Dotnet5 = "net5.0";
            public const string Dotnet6 = "net6.0";
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {  
            testRootPath = TestContext.CurrentContext.TestDirectory;

            zipRootFolderPath = Path.Combine(testRootPath, "TestProjects");

            clientConfigPath = Path.Combine(testRootPath, "Resources", "porting-assistant-config.json");

            tempProjectRoot = Path.GetFullPath(Path.Combine(
                Path.GetTempPath(),
                "PortingAssistantExtension",
                Path.GetRandomFileName()));
 
            Directory.CreateDirectory(tempProjectRoot);

            string[] files = Directory.GetFiles(zipRootFolderPath, "*.zip");
            foreach(string filePath in files)
            {
                string file = Path.GetFileName(filePath);
                if (!testFilesInfo.ContainsKey(file)) continue;

                string[] fileInfo = testFilesInfo[file];

                string testProjectZipFilePath = Path.Combine(zipRootFolderPath, file);
                using (ZipArchive archive = ZipFile.Open(testProjectZipFilePath, ZipArchiveMode.Read))
                {
                    archive.ExtractToDirectory(tempProjectRoot, true);
                }

                testProjectInfoList.Add(fileInfo);
            }
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            ProcessHelper.getInstance().StopServer();

            if (tempProjectRoot != null)
            {
                Directory.Delete(tempProjectRoot, true);
            }

            ProcessHelper.getInstance().StopServer();
        }

        [TestCase(TargetFramework.Dotnet6)]
        [TestCase(TargetFramework.Dotnet5)]
        [TestCase(TargetFramework.DotnetCoreApp31)]
        public async Task TestMvcMusicStoreAsync(string targetFramework)
        {
            InitializeTestResource("MvcMusicStore.zip");
            string[] projectInfo = testProjectInfoList.FindLast(t => t[0].Equals("MvcMusicStore"));
            if (projectInfo == null)
            {
                Assert.IsTrue(false);
                return;
            }

            Boolean result = await TestSolutionAsync(projectInfo, targetFramework);
            Console.WriteLine("Verification TestMvcMusicStore Result: " + result);
            Assert.IsTrue(result);

            Boolean portResult = await TestPortSolutionAsync(projectInfo, targetFramework, false);
            Console.WriteLine("Porting Verification TestMvcMusicStore Result: " + portResult);
            Assert.IsTrue(portResult);
        }

        [TestCase(TargetFramework.Dotnet6)]
        [TestCase(TargetFramework.Dotnet5)]
        [TestCase(TargetFramework.DotnetCoreApp31)]
        public async Task TestMvcMusicStorePortingOnlyNegativeAsync(string targetFramework)
        {
            InitializeTestResource("MvcMusicStore.zip");
            string[] projectInfo = testProjectInfoList.FindLast(t => t[0].Equals("MvcMusicStore"));
            if (projectInfo == null)
            {
                Assert.IsTrue(false);
                return;
            }

            Boolean portResult = await TestPortSolutionAsync(projectInfo, targetFramework, false, false);
            Console.WriteLine("Porting Verification TestMvcMusicStore Result: " + portResult);
            Assert.IsFalse(portResult);
        }

        [TestCase(TargetFramework.Dotnet6)]
        [TestCase(TargetFramework.Dotnet5)]
        [TestCase(TargetFramework.DotnetCoreApp31)]
        public async Task TestMvcMusicStoreWithFixAsync(string targetFramework)
        {
            InitializeTestResource("MvcMusicStore.zip");
            string[] projectInfo = testProjectInfoList.FindLast(t => t[0].Equals("MvcMusicStore"));
            if (projectInfo == null)
            {
                Assert.IsTrue(false);
                return;
            }

            Boolean result = await TestSolutionAsync(projectInfo, targetFramework);
            Console.WriteLine("Verification TestMvcMusicStore Result: " + result);
            Assert.IsTrue(result);

            Boolean portResult = await TestPortSolutionAsync(projectInfo, targetFramework, true);
            Console.WriteLine("Porting Verification TestMvcMusicStore Result: " + portResult);
            Assert.IsTrue(portResult);
        }

        [TestCase(TargetFramework.Dotnet6)]
        [TestCase(TargetFramework.Dotnet5)]
        [TestCase(TargetFramework.DotnetCoreApp31)]
        public async Task TestMiniblogCoreAsync(string targetFramework)
        {
            string[] projectInfo = testProjectInfoList.FindLast(t => t[0].Equals("Miniblog.Core-master"));
            if (projectInfo == null)
            {
                Assert.IsTrue(false);
                return;
            }

            Boolean result = await TestSolutionAsync(projectInfo, targetFramework);
            Console.WriteLine("Verification MiniblogCore Result: " + result);
            Assert.IsTrue(result);

            Boolean portResult = await TestPortSolutionAsync(projectInfo, targetFramework, false);
            Console.WriteLine("Porting Verification TestMiniblogCore Result: " + portResult);
            Assert.IsTrue(portResult);

        }

        [Test]
        public async Task TestMvcMusicStoreNet50Async()
        {
            InitializeTestResource("MvcMusicStore.zip");
            string[] projectInfo = testProjectInfoList.FindLast(t => t[0].Equals("MvcMusicStore"));
            if (projectInfo == null)
            {
                Assert.IsTrue(false);
                return;
            }

            Boolean result = await TestSolutionAsync(projectInfo, NET50);
            Console.WriteLine("Verification TestMvcMusicStore Result: " + result);
            Assert.IsTrue(result);

            Boolean portResult = await TestPortSolutionAsync(projectInfo, NET50, false);
            Console.WriteLine("Porting Verification TestMvcMusicStore Result: " + portResult);
            Assert.IsTrue(portResult);
        }

        [TestCase(TargetFramework.Dotnet6)]
        [TestCase(TargetFramework.Dotnet5)]
        [TestCase(TargetFramework.DotnetCoreApp31)]
        public async Task TestNopCommerceAsync(string targetFramework)
        {
            InitializeTestResource("NopCommerce-3.1.zip");
            string[] projectInfo = testProjectInfoList.FindLast(t => t[0].Equals("NopCommerce-3.1"));
            if (projectInfo == null)
            {
                Assert.IsTrue(false);
                return;
            }

            Boolean result = await TestSolutionAsync(projectInfo, targetFramework);
            Console.WriteLine("Verification Test NopCommerce Result: " + result);
            Assert.IsTrue(result);
        }

        [TestCase(TargetFramework.Dotnet6)]
        [TestCase(TargetFramework.Dotnet5)]
        [TestCase(TargetFramework.DotnetCoreApp31)]
        public async Task TestPortSingleProjectAsync(string targetFramework)
        {
            string[] projectInfo = testProjectInfoList.FindLast(t => t[0].Equals("StarWars"));
            if (projectInfo == null)
            {
                Assert.IsTrue(false);
                return;
            }
            Boolean portResult = await TestPortSolutionAsync(projectInfo,  targetFramework, true, true, "StarWars.Core");
            Console.WriteLine("Porting Verification StarWars Result: " + portResult);
            Assert.IsTrue(portResult);
        }
        private void InitializeTestResource(string testSolutionName)
        {
            string testProjectZipFilePath = Path.Combine(zipRootFolderPath, testSolutionName);
            using (ZipArchive archive = ZipFile.Open(testProjectZipFilePath, ZipArchiveMode.Read))
            {
                archive.ExtractToDirectory(tempProjectRoot, true);
            }
        }

        private async Task<Boolean> TestSolutionAsync(string[] projectInfo, string targetFramework)
        {
            PAIntegTestClient client = null;
            try
            {
                string solutionPath = Path.Combine(tempProjectRoot, projectInfo[0]);
                string solutionName = projectInfo[1];
                string jsonFile = Path.Combine(zipRootFolderPath, Path.GetFileNameWithoutExtension(projectInfo[2]) + $"-{targetFramework}" + Path.GetExtension(projectInfo[2]));
                
                StartLanguageServer();

                client = new PAIntegTestClient(solutionPath, solutionName);

                await client.InitClientAsync();
                var currentResults = await client.AssessSolutionAsync(targetFramework);

                /* Uncomment this method to save the results in a json file*/
                //SaveResults(jsonFile, currentResults);
                
                Boolean result = VerifyResults(jsonFile, currentResults);
                Console.WriteLine("Verification Result: " + result);
                return result;

            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
            finally
            {
                if (client != null)
                {
                    try
                    {
                        await client.CleanupAsync();
                    }
                    catch (Exception e) { }
                }

                try
                {
                    ProcessHelper.getInstance().StopServer();
                }
                catch (Exception e) { }
            }

            return false;
        }

        private async Task<Boolean> TestPortSolutionAsync(string[] projectInfo,  string targetFramework, bool includeFix,  bool assessSolution=true, string projectName = "")
        {
            PAIntegTestClient client = null;
            try
            { 
                string solutionPath = Path.Combine(tempProjectRoot, projectInfo[0]);
                string solutionName = projectInfo[1];
                string expectedSolutionPath = solutionPath;

                StartLanguageServer();

                client = new PAIntegTestClient(solutionPath, solutionName);

                await client.InitClientAsync();
                if (assessSolution)
                {
                    await client.AssessSolutionAsync(targetFramework);
                }
                var currentResults = await client.PortSolutionAsync(targetFramework, includeFix, projectName);
                if (includeFix)
                {
                    expectedSolutionPath += "-WithFix";
                }
                if(targetFramework.Equals(NET50))
                {
                    expectedSolutionPath += "-net50";
                }
                expectedSolutionPath += "-PortResults";


                Boolean result = FileUtils.AreTwoDirectoriesEqual(solutionPath, expectedSolutionPath);
                return result;

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
            finally
            {
                if (client != null)
                {
                    try
                    {
                        await client.CleanupAsync();
                    }
                    catch (Exception e) { }
                }

                try
                {
                    ProcessHelper.getInstance().StopServer();
                }
                catch (Exception e) { }
            }

            return false;
        }
        private static void SaveResults(string resultsFile, AnalysisTestResult analysisResults)
        {
            JsonUtils.ToJsonFile(analysisResults, resultsFile);
        }

        private static bool VerifyResults(string resultsFile, AnalysisTestResult analysisResults)
        {
            AnalysisTestResult xResult = JsonUtils.FromJsonFile(resultsFile);

            ISet<string> currentResults = analysisResults.GetCompatResultsAsSet();
            ISet<string> expectedResults = xResult.GetCompatResultsAsSet();
            Boolean verifyResult = currentResults.Count == expectedResults.Count;
            int count = 0;
            foreach (var eResult in expectedResults)
            {
                if (!currentResults.Contains(eResult))
                {
                    Console.WriteLine("MISSING:  " + eResult);
                    verifyResult = false;
                }
                else
                {
                    count++;
                    currentResults.Remove(eResult);
                }
            }

            Console.WriteLine($"Expected Count {expectedResults.Count}; Found count: {count}; Not in expected: {currentResults.Count}");

            if (!verifyResult)
            {
                Console.WriteLine("New entries found in current run: " + currentResults.Count);
                if (currentResults.Count > 0)
                {
                    foreach (var result in currentResults)
                    {
                        Console.WriteLine("NEW " + result);
                    }
                }
            }

            return verifyResult;
        }
        
        private void StartLanguageServer()
        {
            ProcessHelper.getInstance().StartServer(testRootPath, clientConfigPath);
        }
    }
}