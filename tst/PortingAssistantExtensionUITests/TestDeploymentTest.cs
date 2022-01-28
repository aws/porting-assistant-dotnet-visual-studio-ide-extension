using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.ECR;
using Amazon.ECR.Model;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using System.Net;
using System.Net.Http;

namespace PortingAssistantExtensionUITests
{
    [TestClass]
    public class TestDeploymentTest : VisualStudioSession
    {
        private readonly string solutionFile;
        private readonly string publishArtifactsDir;
        private string profileName;
        private string regionName;
        
        // .cs file to trigger langauge server
        private const string targetCsFile = "Program.cs";
        private const string deploymentName = "TestCoreMvcApp";

        public TestDeploymentTest()
        {
            solutionFile = $"{testSolutionsDir}\\CoreMVC\\CoreMVC.sln";
            publishArtifactsDir = $"{testSolutionsDir}\\coremvc-publish";
        }

        [TestMethod]
        public void RunDeployment()
        {
            GoToFile(targetCsFile);
            ClickPortingAssistantMenuElement(testDeploymentActionText);
            WaitForElementWithId(buildArtifactsDirTextBoxId, 120);
            var submitButton = session.FindElementByXPath(deployButtonXPath);

            AwsProfileBoxChecks(submitButton);
            BuildArtifactsDirBoxChecks(submitButton);
            DeployNameBoxChecks(submitButton);

            submitButton.Click(); 
            WaitForElement(deploymentSuccessXPath, 600);
            var deployedTextElement = session.FindElementByXPath(deploymentSuccessXPath);
            string url = deployedTextElement.Text.Substring(deployedTextElement.Text.IndexOf("http:"));
            string html = GetHtml(url);
            Assert.AreEqual(html, ExpectedValues.TestDeployHtml);

            ClickPortingAssistantMenuElement(testDeploymentMenuText);
            WaitForElement(currentApplicationsWindowXPath);
            string deployedAppUrl = session.FindElementByXPath(GetEndPointXPath(url)).Text;
            Assert.AreEqual(url + '/', deployedAppUrl);
            session.FindElementByXPath(currentApplicationsWindowClose).Click();
        }

        private static void DeployNameBoxChecks(OpenQA.Selenium.Appium.Windows.WindowsElement submitButton)
        {
            var deployNameTextBox = session.FindElementByAccessibilityId(deploymentNameBoxId);
            deployNameTextBox.Clear();
            submitButton.Click();
            WaitForElement(applicationNameErrorText);
            deployNameTextBox.SendKeys("a");
            submitButton.Click();
            WaitForElement(applicationNameErrorText);
            deployNameTextBox.Clear();
            deployNameTextBox.SendKeys("@@@@");
            submitButton.Click();
            WaitForElement(applicationNameErrorText);
            deployNameTextBox.Clear();
            deployNameTextBox.SendKeys(deploymentName);
        }

        private void BuildArtifactsDirBoxChecks(OpenQA.Selenium.Appium.Windows.WindowsElement submitButton)
        {
            var buildArtifactsDirTextBox = session.FindElementByAccessibilityId(buildArtifactsDirTextBoxId);
            buildArtifactsDirTextBox.Clear();
            submitButton.Click();
            WaitForElement(buildFolderErrorXPath);
            buildArtifactsDirTextBox.SendKeys(publishArtifactsDir);
        }

        private void AwsProfileBoxChecks(OpenQA.Selenium.Appium.Windows.WindowsElement submitButton)
        {
            var awsProfileBox = session.FindElementByAccessibilityId(awsProfileBoxId);
            awsProfileBox.Click();
            //Escape clears the combobox
            new Actions(session).SendKeys(Keys.Escape).Perform();
            submitButton.Click();
            WaitForElementWithId(profileErrorHintId);
            awsProfileBox.Click();
            awsProfileBox.SendKeys(Keys.Down);
            awsProfileBox.SendKeys(Keys.Enter);
            
            //save region and profile for cleanup
            profileName = awsProfileBox.Text;
            regionName = session.FindElementByAccessibilityId(awsRegionBoxId).Text;
        }

        [TestInitialize]
        public void ClassInitialize()
        {
            Setup(solutionFile);
        }

        [TestCleanup]
        public void ClassCleanup()
        {
            TearDown();
            CleanUpCloudFormation();
        }

        private void CleanUpCloudFormation()
        {
            var chain = new CredentialProfileStoreChain();
            var region = Amazon.RegionEndpoint.GetBySystemName(regionName);

            if (chain.TryGetAWSCredentials(profileName, out AWSCredentials credentials))
            {
                var cfClient = new AmazonCloudFormationClient(credentials, region);
                var ecrClient = new AmazonECRClient(credentials, region);

                var listResponse = cfClient.ListStacks(new ListStacksRequest()
                {
                    StackStatusFilter = new List<string>() { StackStatus.CREATE_COMPLETE.ToString() }
                });

                foreach (StackSummary summary in listResponse.StackSummaries)
                {
                    if (summary.TemplateDescription == "This CloudFormation template deploys an ECR repo and CodeBuild Project.")
                    {
                        var deleteRepoResponse = ecrClient.DeleteRepository(new DeleteRepositoryRequest { RepositoryName = summary.StackName, Force = true });
                        Assert.AreEqual(HttpStatusCode.OK, deleteRepoResponse.HttpStatusCode);
                    }
                    var response = cfClient.DeleteStack(new DeleteStackRequest 
                    {
                        StackName = summary.StackName
                    });
                    Assert.AreEqual(HttpStatusCode.OK, response.HttpStatusCode);
                }
                
            }
        }

        private static string GetHtml(string url)
        {
            string result;
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = client.GetAsync(url).Result)
                {
                    using (HttpContent content = response.Content)
                    {
                        result = content.ReadAsStringAsync().Result;
                    }
                }
            }
            return result;
        }

        //Tries to target endpoint in current deployed apps table. 
        private static string GetEndPointXPath(string url)
        {
            return $"//Text[@ClassName=\"TextBlock\"][@Name=\"{url}/\"]";
        }

        #region AutomationId and XPath Constants
        private const string applicationNameErrorText = "//Text[@Name=\"ERROR: Please fix application name\"][@AutomationId=\"DeploymentNameErrorHint\"]/Text[@ClassName=\"TextBlock\"][@Name=\"ERROR: Please fix application name\"]";
        private const string deploymentSuccessXPath = "//Text[starts-with(@Name,\"Deployed your project on AWS, please view endpoint: \")][@AutomationId=\"maintext\"]";
        private const string buildFolderErrorXPath = "//Text[@Name=\"ERROR: Build folder with artifacts not found\"][@AutomationId=\"BuildArtifactsDirBrowserButtonErrorHint\"]/Text[@ClassName=\"TextBlock\"][@Name=\"ERROR: Build folder with artifacts not found\"]";
        private const string currentApplicationsWindowClose = "//Window[@ClassName =\"Window\"][@Name=\"Applications Running on AWS\"]/TitleBar[@AutomationId=\"TitleBar\"]/Button[@Name=\"Close\"][@AutomationId=\"Close\"]";
        //private const string appUrlElementXPath = "/Custom[@ClassName=\"DataGridCell\"][@Name=\" \"]/Text[@ClassName=\"TextBlock\"][@Name=\" \"]/Hyperlink[@ClassName=\"Hyperlink\"][@Name=\" \"]/Text[@ClassName=\"TextBlock\"][starts-with(@Name,\"http://\")]";

        private const string testDeploymentMenuText = "View Running Applications on AWS";
        private const string currentApplicationsWindowXPath = "//Window[@ClassName =\"Window\"][@Name=\"Applications Running on AWS\"]";
        private const string testDeploymentActionText = "Test Applications on AWS";

        private const string deployButtonXPath = "//Button[@ClassName=\"Button\"][@Name=\"Test on AWS\"]";

        private const string buildArtifactsDirTextBoxId = "BuildArtifactsDirBrowseTextBox";
        private const string deploymentNameBoxId = "DeploymentNameTextBox";
        private const string awsProfileBoxId = "AwsProfileComboBox";
        private const string profileErrorHintId = "ProfileErrorHint";
        private const string awsRegionBoxId = "AwsRegionBox";

        #endregion
    }
}
