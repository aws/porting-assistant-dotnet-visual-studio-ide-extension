using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            return dte.Solution.FullName;
        }

        public static  List<string>  GetProjectPath(string solutionPath)
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
    }
}
