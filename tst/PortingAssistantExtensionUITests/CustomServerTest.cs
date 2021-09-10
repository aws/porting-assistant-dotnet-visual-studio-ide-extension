//******************************************************************************
//
// Copyright (c) 2017 Microsoft Corporation. All rights reserved.
//
// This code is licensed under the MIT License (MIT).
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//******************************************************************************

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using OpenQA.Selenium;
using System;
using System.Threading;

namespace PortingAssistantExtensionUITests
{
    [TestClass]
    public class CustomServerTest : VisualStudioSession 
    {
        private const string portingResultsFile = @"C:\testsolutions\CustomServer\PortSolutionResult.json";
        private const string solutionFile = @"C:\testsolutions\CustomServer\CustomServer.sln";

        [TestMethod]
        public void RunTest()
        {
            GoToFile("startup.cs");
            StartFullSolutionAssessment();
            WaitForElement("//Pane[starts-with(@Name,\"Assessment successful. You can view the assessment results in th\")]");
            WaitForElement("//DataItem[@ClassName=\"ListViewItem\"][starts-with(@Name,\"PA0002. Add a reference to Microsoft.AspNetCore.Owin and remove \")]");
            PortSolution();
            VerifyPortingResults(ExpectedValues.MvcMusicStorePortSolution, File.ReadAllText(portingResultsFile));
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
        }
    }
}
