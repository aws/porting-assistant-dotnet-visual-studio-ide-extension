using CTA.Rules.Models;
using PortingAssistant.Client.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistantExtensionUnitTest
{
    public class TestParameters
    {
        private static readonly string DEFAULT_TARGET = "netcoreapp3.1";

        public static readonly PackageVersionPair TestPackageVersionPair = new PackageVersionPair
        {
            PackageId = "System.Web.Mvc",
            PackageSourceType = PackageSourceType.NUGET,
            Version = "5.2.7"
        };

        public static readonly RecommendedAction TestReplaceNamespaceRecommendedAction = new RecommendedAction
        {
            Description = "Replace System.Web.Mvc namespace with Microsoft.AspNetCore.Mvc.",
            RecommendedActionType = RecommendedActionType.ReplaceNamespace,
            TargetCPU = new List<string> { "x86", "x64", "ARM32", "ARM64" },
            TextChanges = new List<TextChange>
                    {
                        new TextChange
                        {
                            FileLinePositionSpan = new Microsoft.CodeAnalysis.FileLinePositionSpan(),
                            NewText = "using Microsoft.AspNetCore.Mvc"

                        }
                    },
            TextSpan = new TextSpan
            {
                EndCharPosition = 22,
                EndLinePosition = 5,
                StartCharPosition = 1,
                StartLinePosition = 5
            }
        };

        public static readonly RecommendedAction TestUpgradePackageRecommendedAction = new RecommendedAction
        {
            RecommendedActionType = RecommendedActionType.UpgradePackage
        };

        private static readonly Dictionary<PackageVersionPair, Task<PackageAnalysisResult>> _packageAnalysisResult = new Dictionary<PackageVersionPair, Task<PackageAnalysisResult>>
        {
            {
                TestPackageVersionPair, Task.FromResult(new PackageAnalysisResult
                {
                    CompatibilityResults = new Dictionary<string, CompatibilityResult>
                    {
                        {
                            DEFAULT_TARGET, new CompatibilityResult
                            {
                                Compatibility = Compatibility.COMPATIBLE,
                                CompatibleVersions = new List<string> { "12.0.3", "12.0.4" }
                            }
                        }
                    },
                    PackageVersionPair = TestPackageVersionPair,
                    Recommendations = new PortingAssistant.Client.Model.Recommendations
                    {
                        RecommendedActions = new List<RecommendedAction>
                        {
                            new RecommendedAction
                            {
                                Description = "12.0.3",
                                RecommendedActionType = RecommendedActionType.UpgradePackage,
                                TargetCPU = null,
                                TextChanges = null,
                                TextSpan = null
                            }
                        }
                    }
                })
            }
        };

        public static readonly SourceFileAnalysisResult TestSourceFileAnalysisResult = new SourceFileAnalysisResult
        {
            SourceFileName = "test",
            SourceFilePath = "/test/test",
            ApiAnalysisResults = new List<ApiAnalysisResult>
            {
                new ApiAnalysisResult
                {
                    CodeEntityDetails = new CodeEntityDetails
                    {
                        CodeEntityType = CodeEntityType.Namespace,
                        Name = "View",
                        Namespace = "System.Web.Mvc",
                        OriginalDefinition = "System.Web.Mvc.Controller.View()",
                        Package = TestPackageVersionPair,
                        Signature = "System.Web.Mvc.Controller.View()",
                        TextSpan = new TextSpan
                        {
                            EndCharPosition = 26,
                            EndLinePosition = 13,
                            StartCharPosition = 20,
                            StartLinePosition = 13
                        }
                    },
                    CompatibilityResults = new Dictionary<string, CompatibilityResult>
                    {
                        { DEFAULT_TARGET, new CompatibilityResult {
                            Compatibility = Compatibility.INCOMPATIBLE,
                            CompatibleVersions = new List<string> { }
                        } }
                    },
                    Recommendations = new PortingAssistant.Client.Model.Recommendations
                    {
                        RecommendedActions = new List<PortingAssistant.Client.Model.RecommendedAction>
                        {
                            new ApiRecommendation
                            {
                                CodeEntityDetails = null,
                                Description = null,
                                RecommendedActionType = RecommendedActionType.NoRecommendation,
                                TargetCPU = null,
                                TextChanges = null,
                                TextSpan = null
                            },
                            new RecommendedAction
                            {
                                Description = "12.0.3",
                                RecommendedActionType = RecommendedActionType.ReplaceApi,
                                TargetCPU = null,
                                TextChanges = null,
                                TextSpan = null
                            },
                            new RecommendedAction
                            {
                                Description = "12.0.3",
                                RecommendedActionType = RecommendedActionType.ReplaceNamespace,
                                TargetCPU = null,
                                TextChanges = null,
                                TextSpan = null
                            },
                            new RecommendedAction
                            {
                                Description = "12.0.3",
                                RecommendedActionType = RecommendedActionType.ReplacePackage,
                                TargetCPU = null,
                                TextChanges = null,
                                TextSpan = null
                            },
                            new RecommendedAction
                            {
                                Description = "12.0.3",
                                RecommendedActionType = RecommendedActionType.UpgradePackage,
                                TargetCPU = null,
                                TextChanges = null,
                                TextSpan = null
                            }
                        }
                    }
                }
            },
            RecommendedActions = new List<RecommendedAction>
            {
               TestReplaceNamespaceRecommendedAction
            }
        };

        private static readonly ProjectAnalysisResult _projectAnalysisResult = new ProjectAnalysisResult
        {
            Errors = null,
            ExternalReferences = null,
            IsBuildFailed = false,
            MetaReferences = new List<string>(),
            SourceFileAnalysisResults = new List<SourceFileAnalysisResult> { TestSourceFileAnalysisResult },
            PackageAnalysisResults = _packageAnalysisResult,
            PackageReferences = null,
            PreportMetaReferences = new List<string>(),
            ProjectFilePath = "/testSolution/testProject",
            ProjectGuid = "xxx",
            ProjectName = "testProject",
            ProjectReferences = null,
            ProjectRules = null,
            ProjectType = "KnownToBeMSBuildFormat",
            TargetFrameworks = null
        };

        public static readonly SolutionAnalysisResult TestSolutionAnalysisResult = new SolutionAnalysisResult
        {
            Errors = null,
            FailedProjects = new List<string>(),
            ProjectAnalysisResults = new List<ProjectAnalysisResult> { _projectAnalysisResult },
            SolutionDetails = new SolutionDetails
            {
                FailedProjects = new List<string>(),
                Projects = new List<ProjectDetails>
                {
                    new ProjectDetails
                    {
                        IsBuildFailed = false,
                        ProjectFilePath = "/testSolution/testProject",
                        ProjectGuid = "xxx",
                        ProjectName = "testProject",
                        ProjectReferences = null,
                        ProjectType = "KnownToBeMSBuildFormat",
                        TargetFrameworks = null
                    }
                }
            },
            Version = null
        };
    }
}
