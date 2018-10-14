using System.Windows;

namespace P2P.PeerClient
{
    /// <summary>
    /// Interaction logic for InputNameDialog.xaml
    /// </summary>
    public partial class InputNameDialog : Window
    {
        public InputNameDialog()
        {
            InitializeComponent();
        }

        public string ResponseText
        {
            get { return ResponseTextBox.Text; }
            set { ResponseTextBox.Text = value; }
        }

        private void OKButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ResponseTextBox.Text))
            {
                ResponseTextBox.Text = string.Empty;
            }
            else
            {
                DialogResult = true;
            }
        }

        private void CancelButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
