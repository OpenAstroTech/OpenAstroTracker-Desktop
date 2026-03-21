using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using OATControl.Avalonia.ViewModels;

namespace OATControl.Avalonia
{
    public partial class DlgChooseOat : Window
    {
        private ConnectVM? _vm;

        public DlgChooseOat()
        {
            InitializeComponent();
        }

        public void SetViewModel(ConnectVM vm)
        {
            _vm = vm;
            DataContext = vm;
            vm.CloseRequested += () => Close();
        }

        public bool RunRAAutoHoming => _vm?.RunRAAutoHoming ?? false;
        public bool RunDECAutoHoming => _vm?.RunDECAutoHoming ?? false;
        public bool RunDECOffsetHoming => _vm?.RunDECOffsetHoming ?? false;

        private void OnDeviceDoubleTapped(object? sender, TappedEventArgs e)
        {
            _vm?.OnDeviceDoubleClick();
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            _vm!.Result = false;
            Close();
        }
    }
}
