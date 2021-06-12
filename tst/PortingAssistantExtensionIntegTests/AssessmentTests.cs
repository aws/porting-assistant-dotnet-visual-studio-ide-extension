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
    //[CollectionDefinition("Non-Parallel Collection", DisableParallelization = true)]
    class AssessmentTests
    {
        private string tempProjectRoot;
        private List<string[]> testProjectInfoList = new List<string[]>();
        private string zipRootFolderPath;
        private string testRootPath;
        private static SemaphoreSlim testLock =  new SemaphoreSlim(1);    // Capacity of 3


        [OneTimeSetUp]
        public void OneTimeSetUp()
        {  
            var testFilesInfo = new Dictionary<string, string[]>()
            {   // Zipfile = {folder-name, solution file, json file}
                ["Miniblog.Core-master.zip"] = new string[] { "Miniblog.Core-master", "Miniblog.Core.sln", "Miniblog.Core.json"},
                ["MvcMusicStore.zip"] = new string[] { "MvcMusicStore", "MvcMusicStore.sln", "MvcMusicStore.json" },
                ["NetFrameworkExample.zip"] = new string[] { "NetFrameworkExample", "NetFrameworkExample.sln", "NetFrameworkExample.json" }
            };

            zipRootFolderPath = Path.Combine(
                TestContext.CurrentContext.TestDirectory, "TestProjects");
            
            testRootPath = TestContext.CurrentContext.TestDirectory;

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
                    archive.ExtractToDirectory(tempProjectRoot);
                }

                testProjectInfoList.Add(fileInfo);
            }

            //RunCLI();
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            if (tempProjectRoot != null)
            {
                Directory.Delete(tempProjectRoot, true);
            }

            ProcessHelper.getInstance().StopServer();
        }

        /*
          public async Task FrameworkProjectAnalysisProduceExpectedJsonResult()
          {
              Console.WriteLine(testProjectInfoList);


              string solutionPath = @"C:/Users/Administrator/Projects/MvcMusicStore/";
              string solutionName = "MvcMusicStore.sln";
              //solutionPath = @"C:/Users/Administrator/Projects/Umbraco-CMS/src";
              //solutionName = "umbraco.sln";

              foreach (string[] projectInfo in testProjectInfoList)
              {
                  PAIntegTestClient client = new PAIntegTestClient(solutionPath, solutionName);
                  try
                  {
                      solutionPath = Path.Combine(tempProjectRoot, projectInfo[0]);
                      solutionName = projectInfo[1];
                      string jsonFile = Path.Combine(testRootPath, projectInfo[2]);

                      await client.InitClient();
                      var currentResults = await client.AssessSolution();
                      SaveResults(jsonFile, currentResults);
                      Boolean result = VerifyResults(jsonFile, currentResults);
                      Console.WriteLine("Verification Result: " + result);
                      Assert.IsTrue(result);

                  }
                  finally
                  {
                      await client.Cleanup();
                  }
              }
          }
        */

       [Test]
        public async Task TestMvcMusicStoreAsync()
        {

            string[] projectInfo = testProjectInfoList.FindLast(t => t[0].Equals("MvcMusicStore"));
            if (projectInfo == null)
            {
                Assert.IsTrue(false);
                return;
            }

            Boolean result = await TestSolution(projectInfo);
            Console.WriteLine("Verification TestMvcMusicStore Result: " + result);
            Assert.IsTrue(result);

        }

       [Test]
        public async Task TestMiniblogCoreAsync()
        {
            string[] projectInfo = testProjectInfoList.FindLast(t => t[0].Equals("Miniblog.Core-master"));
            if (projectInfo == null)
            {
                Assert.IsTrue(false);
                return;
            }

            Boolean result = await TestSolution(projectInfo);
            Console.WriteLine("Verification MiniblogCore Result: " + result);
            Assert.IsTrue(result);

        }

       [Test]
        public async Task TestNetFrameworkExampleAsync()
        {
            string[] projectInfo = testProjectInfoList.FindLast(t => t[0].Equals("NetFrameworkExample"));
            if (projectInfo == null)
            {
                Assert.IsTrue(false);
                return;
            }

            Boolean result = await TestSolution(projectInfo);
            Console.WriteLine("Verification NetFrameworkExample Result: " + result);
            Assert.IsTrue(result);

        }


        private async Task<Boolean> TestSolution(string[] projectInfo)
        {
            // testLock.Wait();
            

            PAIntegTestClient client = null;
            try
            {
              //  testLock.Wait();
                string solutionPath = Path.Combine(tempProjectRoot, projectInfo[0]);
                string solutionName = projectInfo[1];
                string jsonFile = Path.Combine(zipRootFolderPath, projectInfo[2]);

               // solutionPath = @"C:/Users/Administrator/Projects/MvcMusicStore/";
               // solutionPath = "C:\\Users\\Administrator\\AppData\\Local\\Temp\\2\\PortingAssistantExtension\\wrehmvz5.ugz\\MvcMusicStore";
               // solutionName = "MvcMusicStore.sln";

                string inPipe = "InPipe" + Path.GetFileNameWithoutExtension(solutionName);
                string outPipe = "OutPipe" + Path.GetFileNameWithoutExtension(solutionName);

                RunCLI(inPipe, outPipe);

                client = new PAIntegTestClient(solutionPath, solutionName, inPipe, outPipe);

                await client.InitClient();
                var currentResults = await client.AssessSolution();
                //SaveResults(jsonFile, currentResults);
                Boolean result = VerifyResults(jsonFile, currentResults);
                Console.WriteLine("Verification Result: " + result);
                return result;

            }
            finally
            {
                if (client != null)
                {
                    try
                    {
                        await client.Cleanup();
                    }
                    catch (Exception e) { }
                }

                try
                {
                    ProcessHelper.getInstance().StopServer();
                }
                catch (Exception e) { }

                //testLock.Release();

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
                Console.WriteLine("New entries found in curreent run: ");
                Console.WriteLine(currentResults);
            }

            return verifyResult;
        }

        /*
        public static Task RunAsync(this Process process)
        {
            var tcs = new TaskCompletionSource<object>();
            process.EnableRaisingEvents = true;
            process.Exited += (s, e) => tcs.TrySetResult(null);
            // not sure on best way to handle false being returned
            if (!process.Start()) tcs.SetException(new Exception("Failed to start process."));
            return tcs.Task;
        }*/

      

        Process exeProcess;
        private void RunCLI(string inPipe, string outPipe)
        {
            string path = @"C:\Users\Administrator\Projects\porting-assistant-dotnet-visual-studio-ide-extension\src\PortingAssistantVSExtensionClient\Resources\porting-assistant-config.json";

            string args = $"{path} {inPipe.ToLower()} {outPipe.ToLower()}";
            ProcessHelper.getInstance().StartServer(TestContext.CurrentContext.TestDirectory, path);
        }

        private void RunCLI______T()
        { 
            //"C:\\Users\\Administrator\\Projects\\porting-assistant-dotnet-visual-studio-ide-extension\\src\\PortingAssistantExtensionIntegTests\\bin\\Debug\\netcoreapp3.1"
            string path = @"C:\Users\Administrator\Projects\porting-assistant-dotnet-visual-studio-ide-extension\src\PortingAssistantVSExtensionClient\Resources\porting-assistant-config.json";
            ProcessStartInfo startInfo = new ProcessStartInfo("PortingAssistantExtensionServer.exe");
            startInfo.WorkingDirectory = TestContext.CurrentContext.TestDirectory;
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = true;
            //startInfo.RedirectStandardOutput = false;

            //startInfo.CreateNoWindow = false;
            //startInfo.UseShellExecute = false;
            //            startInfo.RedirectStandardOutput = true;
            // startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            //startInfo.RedirectStandardOutput = true;
            startInfo.WindowStyle = ProcessWindowStyle.Normal;//ProcessWindowStyle.Hidden;
            startInfo.Arguments = path;

            //myProcess.StartInfo.FileName = fileName;
            // Allows to raise event when the process is finished
            //startInfo.EnableRaisingEvents = true;
            // Eventhandler wich fires when exited
            //myProcess.Exited += new EventHandler(myProcess_Exited);
            // Starts the process
            //myProcess.Start();

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
               // using (Process exeProcess = Process.Start(startInfo))
                {
                    exeProcess = Process.Start(startInfo);
                    if (!exeProcess.HasExited && exeProcess.Responding)
                    {
                        Console.WriteLine("Successfully started ...");
                    }

                    Console.WriteLine(exeProcess.ExitCode);
                    // do stuff with results
                    //  exeProcess.WaitForExit();
                    /*while (!exeProcess.HasExited && exeProcess.Responding)
                    {
                        Thread.Sleep(1000);
                    } 
                    string output = exeProcess.StandardOutput.ReadToEnd();
                    Console.WriteLine(output);*/

                }
            }
            catch
            {
                Console.WriteLine("Fail to execute PA Client CLI!");
             //   Assert.Fail();
            }
        }

    }
}