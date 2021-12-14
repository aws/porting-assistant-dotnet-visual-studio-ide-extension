using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistantVSExtensionClient.Models
{
    class AnalyzeSolutionResponse
    {
        public int incompatibleAPis;
        public int incompatiblePacakges;
        public bool hasWebFormsProject;
    }
}
