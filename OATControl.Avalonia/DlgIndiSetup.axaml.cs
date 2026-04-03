using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace OATControl.Avalonia
{
    public partial class DlgIndiSetup : Window, INotifyPropertyChanged
    {
        private string _host = "localhost";
        private string _portText = "7624";
        private bool _forceAltAzControls;
        public DlgIndiSetup() : this("localhost", 7624, false) { }

        public DlgIndiSetup(string host, int port, bool forceAltAzControls)
        {
            Host = host;
            PortText = port.ToString();
            ForceAltAzControls = forceAltAzControls;
            Confirmed = false;
            DataContext = this;
            InitializeComponent();
        }

        public bool Confirmed { get; private set; }

        public string Host
        {
            get => _host;
            set { _host = value; OnPropertyChanged(); }
        }

        public string PortText
        {
            get => _portText;
            set { _portText = value; OnPropertyChanged(); }
        }

        public int Port => int.TryParse(_portText, out var p) && p is > 0 and <= 65535 ? p : 7624;

        public bool ForceAltAzControls
        {
            get => _forceAltAzControls;
            set { _forceAltAzControls = value; OnPropertyChanged(); }
        }

        private void OnOkClick(object? sender, RoutedEventArgs e)
        {
            Confirmed = true;
            Close();
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e) => Close();

        public new event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
