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
    public class MvcMusicStoreTest : VisualStudioSession 
    {
        [TestMethod]
        public void RunTest()
        {
            GoToFile("program.cs");
            StartFullSolutionAssessment();
            WaitForElement("//Pane[starts-with(@Name,\"Assessment successful. You can view the assessment results in th\")]");
        }

        [TestInitialize]
        public void ClassInitialize()
        {
            Setup(@"C:\testsolutions\mvcmusicstore\sourceCode\mvcmusicstore\MvcMusicStore.sln");
        }

        [TestCleanup]
        public void ClassCleanup()
        {
            TearDown();
        }
    }
}
