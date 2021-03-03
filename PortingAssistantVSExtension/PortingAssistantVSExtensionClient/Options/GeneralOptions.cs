using System.ComponentModel;

namespace PortingAssistantVSExtensionClient.Options
{
    internal class GeneralOptions : BaseOptionModel<GeneralOptions>
    {
        [Category("General")]
        [DisplayName("Enabled")]
        [Description("Specifies whether to run Assessment automatically or not.")]
        [DefaultValue(true)]
        public bool Enabled { get; set; } = true;
    }
}
