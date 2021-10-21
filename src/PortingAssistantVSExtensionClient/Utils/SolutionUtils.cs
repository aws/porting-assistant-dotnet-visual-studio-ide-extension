using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PortingAssistantVSExtensionClient.Utils
{
    public static class SolutionUtils
    {
        public static async Task<string> GetSolutionPathAsync(DTE2 dte)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            //dte.Solution.SolutionBuild.BuildState == vsBuildState.vsBuildStateDone;
            return dte.Solution.FullName;
        }

        public static string GetSolutionPath(DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return dte.Solution.FullName;
        }

        public static Boolean IsBuildSucceed(DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return dte.Solution.SolutionBuild.BuildState == vsBuildState.vsBuildStateDone
                && dte.Solution.SolutionBuild.LastBuildInfo == 0;
        }

        public static string GetBuildOutputPath(DTE2 dte, string projectPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (string.IsNullOrWhiteSpace(projectPath))
                return null;

            foreach (Project project in dte.Solution.Projects)
            {
                if(project.FullName.Equals(projectPath, StringComparison.OrdinalIgnoreCase))
                {
                    string fullPath = project.Properties.Item("FullPath").Value.ToString();
                    string outputPath = project.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString();
                    string buildOutputPath = Path.Combine(fullPath, outputPath);

                    // check if the build output is not empty
                    if (!Directory.Exists(buildOutputPath) || (Directory.GetFiles(buildOutputPath).Length + Directory.GetDirectories(buildOutputPath).Length == 0))
                    {
                        return null;
                    }
                    return buildOutputPath;
                }
            }
            return null;
        }

        public static async Task<Dictionary<string, List<string>>> GetMetadataReferencesAsync(DTE2 dte)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var metadataReferences = new Dictionary<string, List<string>>();
            var projects = dte.Solution.Projects.OfType<EnvDTE.Project>();
            var allProjects = new List<VSLangProj.VSProject>();

            foreach (var p in projects)
            {
                if (p.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                {
                    var allSubProjects = await LoadProjectsInFolderAsync(p);
                    allProjects.AddRange(allSubProjects);
                    continue;
                }

                VSLangProj.VSProject vsProject = p.Object as VSLangProj.VSProject;
                if (vsProject == null) continue;
                allProjects.Add(vsProject);
            }

            foreach (var vsProject in allProjects)
            {
                VSLangProj.References references = vsProject.References;

                var projectReferences = new List<string>();
                foreach (VSLangProj.Reference reff in references)
                {
                    projectReferences.Add(reff.Path);
                }
                metadataReferences.Add(vsProject.Project.FileName, projectReferences);
            }

            return metadataReferences;
        }

        private static async Task<List<VSLangProj.VSProject>> LoadProjectsInFolderAsync(Project projectOrProjectFolder)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            List<VSLangProj.VSProject> projects = new List<VSLangProj.VSProject>();
            try
            {
                var itemCount = (projectOrProjectFolder.ProjectItems as ProjectItems).Count;
                for (var i = 1; i <= itemCount; i++)
                {
                    var subProject = projectOrProjectFolder.ProjectItems.Item(i).SubProject;
                    if (subProject == null) continue;
                    if (subProject.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                    {
                        var result = await LoadProjectsInFolderAsync(subProject);
                        if (result.Any())
                        {
                            projects.AddRange(result);
                        }
                    }
                    else
                    {
                        var vsLangSubProject = subProject.Object as VSLangProj.VSProject;
                        if (vsLangSubProject != null)
                        {
                            projects.Add(vsLangSubProject);
                        }
                    }
                }
            }
            catch
            {
                //TODO Log this
            }
            return projects;
        }

        public static List<string> GetProjectPath(string solutionPath)
        {
            var Content = File.ReadAllText(solutionPath);
            Regex projReg = new Regex(
                "Project\\(\"\\{[\\w-]*\\}\"\\) = \"([\\w _]*.*)\", \"(.*\\.(cs|vcx|vb)proj)\""
                , RegexOptions.Compiled);
            var matches = projReg.Matches(Content).Cast<Match>();
            var Projects = matches.Select(x => x.Groups[2].Value).ToList();
            for (int i = 0; i < Projects.Count; ++i)
            {
                if (!Path.IsPathRooted(Projects[i]))
                    Projects[i] = Path.Combine(Path.GetDirectoryName(solutionPath),
                        Projects[i]);
                Projects[i] = Path.GetFullPath(Projects[i]);
            }

            return Projects;
        }

        public static string GetSelectedProjectPath()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                IntPtr hierarchyPointer, selectionContainerPointer;
                Object selectedObject = null;
                IVsMultiItemSelect multiItemSelect;
                uint projectItemId;
                IVsMonitorSelection monitorSelection = (IVsMonitorSelection)Package.GetGlobalService(typeof(SVsShellMonitorSelection));
                monitorSelection.GetCurrentSelection(out hierarchyPointer,
                                         out projectItemId,
                                         out multiItemSelect,
                                         out selectionContainerPointer);
                IVsHierarchy selectedHierarchy = Marshal.GetTypedObjectForIUnknown(
                                         hierarchyPointer,
                                         typeof(IVsHierarchy)) as IVsHierarchy;
                if (selectedHierarchy != null)
                {
                    Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(selectedHierarchy.GetProperty(
                                                      projectItemId,
                                                      (int)__VSHPROPID.VSHPROPID_ExtObject,
                                                      out selectedObject));
                    Project selectedProject = selectedObject as Project;
                    if (selectedProject != null) return selectedProject.FileName;
                }
                return "";
            }
            catch (Exception e)
            {
                return "";
            }
        }

        public static string GetTempDirectory(string pathToSolution)
        {
            if (pathToSolution != null)
            {
                string solutionId;
                using (var sha = new SHA256Managed())
                {
                    byte[] textData = System.Text.Encoding.UTF8.GetBytes(pathToSolution);
                    byte[] hash = sha.ComputeHash(textData);
                    solutionId = BitConverter.ToString(hash);
                }
                var tempSolutionDirectory = Path.Combine(Path.GetTempPath(), solutionId);
                tempSolutionDirectory = tempSolutionDirectory.Replace("-", "");
                return tempSolutionDirectory;
            }
            return null;
        }
    }
}
