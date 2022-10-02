using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistantVSExtensionClient.Models
{
    class AnalyzeSolutionResponse
    {
        public int incompatibleNugetPackages;
        public int portableNugetPackages;
        public int totalNugetPackages;
        public int incompatibleAPis;
        public int portableAPis;
        public int totalApis;
        public int portableProjects;
        public int totalProjects;
        public bool hasWebFormsProject = false;
    }
}
