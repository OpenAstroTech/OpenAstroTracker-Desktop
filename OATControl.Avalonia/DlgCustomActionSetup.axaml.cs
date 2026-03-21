using Avalonia.Controls;
using Avalonia.Interactivity;

namespace OATControl.Avalonia
{
    public partial class DlgCustomActionSetup : Window
    {
        public string ButtonText { get; set; } = string.Empty;
        public string CommandText { get; set; } = string.Empty;
        public bool? DialogResult { get; private set; }

        public DlgCustomActionSetup()
        {
            InitializeComponent();
        }

        protected override void OnOpened(System.EventArgs e)
        {
            base.OnOpened(e);
            ButtonTextBox.Text = ButtonText;
            CommandTextBox.Text = CommandText;
        }

        private void OnOkClick(object? sender, RoutedEventArgs e)
        {
            ButtonText = ButtonTextBox.Text ?? string.Empty;
            CommandText = CommandTextBox.Text ?? string.Empty;
            DialogResult = true;
            Close();
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
