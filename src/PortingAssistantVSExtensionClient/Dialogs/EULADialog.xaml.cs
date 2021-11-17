using Microsoft.VisualStudio.PlatformUI;

namespace PortingAssistantVSExtensionClient.Dialogs
{
    /// <summary>
    /// Interaction logic for EULADialog.xaml
    /// </summary>
    public partial class EULADialog : DialogWindow
    {
        public bool ClickResult = false;
        public EULADialog()
        {
            InitializeComponent();
            this.Title = "License agreement";
        }

        public static bool EnsureExecute(string eula)
        {
            EULADialog eulaDialog = new EULADialog();
            eulaDialog.eulaContent.Text = eula;
            eulaDialog.ShowModal();
            return eulaDialog.ClickResult;
        }

        private void Cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void Submit_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (AgreeTermsCheck.IsChecked.HasValue && AgreeTermsCheck.IsChecked.Value)
            {
                ClickResult = true;
                Close();
            }
        }
    }
}
