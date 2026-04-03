using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using OATCommunications.Utilities;
using OATControl.ViewModels;

namespace OATControl.Avalonia
{
    public partial class MainWindow : Window
    {
        private readonly MountVM mountVm;

        public MainWindow()
        {
            mountVm = new MountVM();
            InitializeComponent();
            DataContext = mountVm;

            Opened += OnWindowOpened;
            Closing += OnWindowClosing;

            AddHandler(InputElement.KeyDownEvent, OnWindowKeyDownForwardToMiniController, RoutingStrategies.Tunnel, true);
            AddHandler(InputElement.KeyUpEvent, OnWindowKeyUpForwardToMiniController, RoutingStrategies.Tunnel, true);

            var savedPos = AppSettings.Instance.WindowPos;
            Position = new PixelPoint((int)Math.Max(0, savedPos.X), (int)Math.Max(0, savedPos.Y));
        }

        private void OnWindowKeyDownForwardToMiniController(object? sender, KeyEventArgs e)
        {
            if (!mountVm.IsMiniControllerVisible)
            {
                return;
            }

            if (e.Source is TextBox)
            {
                return;
            }

            if (mountVm.HandleMiniControllerKeyDown(e.Key))
            {
                Log.WriteLine("UI: MainWindow forwarded KeyDown {0} to mini controller", e.Key);
                e.Handled = true;
            }
        }

        private void OnWindowKeyUpForwardToMiniController(object? sender, KeyEventArgs e)
        {
            if (!mountVm.IsMiniControllerVisible)
            {
                return;
            }

            if (e.Source is TextBox)
            {
                return;
            }

            if (mountVm.HandleMiniControllerKeyUp(e.Key))
            {
                Log.WriteLine("UI: MainWindow forwarded KeyUp {0} to mini controller", e.Key);
                e.Handled = true;
            }
        }

        private void OnWindowOpened(object? sender, EventArgs e)
        {
            mountVm.OnAppBooted();
        }

        private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
        {
            mountVm.Disconnect();
            AppSettings.Instance.WindowPos = (Position.X, Position.Y);
            AppSettings.Instance.Save();
        }

        private void OnConnectButtonPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            mountVm.ForceShowDialog = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
        }

        private void OnTargetTextBoxGotFocus(object? sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.SelectAll();
                mountVm.SetFocusTarget(textBox.Tag?.ToString() ?? string.Empty);
            }
        }

        private void OnTargetTextBoxLostFocus(object? sender, RoutedEventArgs e)
        {
            mountVm.SetFocusTarget(string.Empty);
        }

        private void OnTargetTextBoxKeyUp(object? sender, KeyEventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                return;
            }

            string? tag = textBox.Tag?.ToString();
            if (string.IsNullOrEmpty(tag))
            {
                return;
            }

            if (e.Key == Key.Up)
            {
                mountVm.OnAdjustTarget(tag + "+");
            }
            else if (e.Key == Key.Down)
            {
                mountVm.OnAdjustTarget(tag + "-");
            }
        }
    }
}
