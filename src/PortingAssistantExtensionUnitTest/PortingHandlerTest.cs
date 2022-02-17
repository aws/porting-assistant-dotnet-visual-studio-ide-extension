using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PortingAssistant.Client.Client;
using PortingAssistant.Client.Model;
using PortingAssistantExtensionServer.Handlers;
using PortingAssistantExtensionServer.Models;
using PortingAssistantExtensionServer.Services;
using PortingAssistantExtensionUnitTest.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestParameters = PortingAssistantExtensionUnitTest.Common.TestParameters;

namespace PortingAssistantExtensionUnitTest
{
    public class PortingHandlerTest
    {
        private Mock<ILogger<PortingHandler>> _logger;
        private PortingService _portingService;

        private Mock<ILogger<PortingService>> _serviceLogger;
        private Mock<IPortingAssistantClient> _clientMock;

        private PortingHandler _portingHandler;

        private readonly ProjectFilePortingRequest _portingRequest = new ProjectFilePortingRequest
        {
            TargetFramework = "netcoreapp3.1",
            ProjectPaths = new List<string> { "/test/testPath" },
            Projects = new List<ProjectDetails>
            {
                new ProjectDetails
                {
                    IsBuildFailed = false,
                    PackageReferences = new List<PackageVersionPair> { TestParameters.TestPackageVersionPair },
                    ProjectFilePath = "/testSolution/testProject",
                    ProjectGuid = "xxx",
                    ProjectName = "testProject",
                    ProjectReferences = null,
                    ProjectType = "KnownToBeMSBuildFormat",
                    TargetFrameworks = null
                }
            },
            IncludeCodeFix = true,
            RecommendedActions = null,
            SolutionPath = "/testSolution/"
        };

        private readonly ProjectFilePortingRequest _portingRequestWithRecommendedAction = new ProjectFilePortingRequest
        {
            TargetFramework = "netcoreapp3.1",
            ProjectPaths = new List<string> { "/test/testPath" },
            Projects = new List<ProjectDetails>
            {
                new ProjectDetails
                {
                    IsBuildFailed = false,
                    PackageReferences = new List<PackageVersionPair> { TestParameters.TestPackageVersionPair },
                    ProjectFilePath = "/testSolution/testProject",
                    ProjectGuid = "xxx",
                    ProjectName = "testProject",
                    ProjectReferences = null,
                    ProjectType = "KnownToBeMSBuildFormat",
                    TargetFrameworks = null
                }
            },
            IncludeCodeFix = true,
            RecommendedActions = new List<RecommendedAction> { TestParameters.TestUpgradePackageRecommendedAction },
            SolutionPath = "/testSolution/"
        };

        private readonly List<PortingResult> _portingResults = new List<PortingResult>
        {
            new PortingResult
            {
                Success = true,
                Message = "Test Project Ported.",
                ProjectFile = "testProject.csproj",
                ProjectName = "/testSolution/testProject",
                Exception = null
            }
        };


        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _logger = new Mock<ILogger<PortingHandler>>();
            _serviceLogger = new Mock<ILogger<PortingService>>();
            _clientMock = new Mock<IPortingAssistantClient>();

            _portingService = new PortingService(_serviceLogger.Object, _clientMock.Object);
            _portingService.ProjectPathToDetails = new Dictionary<string, ProjectDetails>()
            {
                {"/testSolution/testProject",
                    new ProjectDetails {
                        ProjectFilePath = "/testSolution/testProject",
                        PackageReferences = new List<PackageVersionPair>
                        {
                            new PackageVersionPair
                            {
                                PackageId = "Newtonsoft.Json",
                                Version = "9.0.1"
                            }

                },
                        ProjectGuid = "testguid",
                        ProjectName="testProject",
                        ProjectReferences = new List<ProjectReference>()
                    }
                }
            };
            _portingHandler = new PortingHandler(_logger.Object, _portingService);
        }

        [SetUp]
        public void Setup()
        {
            _clientMock.Setup(_client => _client.ApplyPortingChanges(It.IsAny<PortingRequest>()))
                .Returns(_portingResults);

        }

        [Test]
        public async Task PortingHandlerSuccessAsync()
        {
            var actualResult = await _portingHandler.Handle(_portingRequest, CancellationToken.None);
            Assert.AreEqual(actualResult.Success, true);
            Assert.AreEqual(actualResult.messages.Count, 1);
            Assert.AreEqual(actualResult.messages[0], "Test Project Ported.");
            Assert.AreEqual(actualResult.SolutionPath, "/testSolution/");

            _clientMock.Verify(_clientMock => _clientMock.ApplyPortingChanges(It.IsAny<PortingRequest>()), Times.Exactly(1));

            _portingService.Dispose();

        }

        [Test]
        public async Task PortingWithRecommendedActionHandlerSuccessAsync()
        {
            var actualResult = await _portingHandler.Handle(_portingRequestWithRecommendedAction, CancellationToken.None);
            Assert.AreEqual(actualResult.Success, true);
            Assert.AreEqual(actualResult.messages.Count, 1);
            Assert.AreEqual(actualResult.messages[0], "Test Project Ported.");
            Assert.AreEqual(actualResult.SolutionPath, "/testSolution/");

            _clientMock.Verify(_clientMock => _clientMock.ApplyPortingChanges(It.IsAny<PortingRequest>()), Times.Exactly(2));

            _portingService.Dispose();

        }
    }
}
