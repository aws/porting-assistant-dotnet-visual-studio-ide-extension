
using Microsoft.VisualStudio.Shell;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PortingAssistantVSExtensionClient.Options
{
    [Guid("459594a1-6b43-4e64-a335-13b1b5581836")]
    public class PortingAssistantOptions: DialogPage
    {
        private string optionEmail = "";
        [Category("Porting Assistant Extension")]
        [DisplayName("Email")]
        [Description("Customer Email")]
        public string OptionEmail
        {
            get { return optionEmail; }
            set { optionEmail = value; }
        }
    }
}
