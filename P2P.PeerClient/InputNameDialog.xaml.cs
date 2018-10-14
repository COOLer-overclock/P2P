using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

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
            ResponseTextBox.KeyDown += new KeyEventHandler(InputNameKeyDown);
            ResponseTextBox.Focus();
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

        void InputNameKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    OkButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    break;
                case Key.Escape:
                    CancelButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    break;
            }
        }
    }
}
