using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using OATControl.ViewModels;

namespace OATControl.Avalonia
{
    public partial class SettingsDialog : Window
    {
        private readonly DispatcherTimer _dispatchTimer;

        public SettingsDialog()
        {
            InitializeComponent();
            _dispatchTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _dispatchTimer.Tick += OnTimer;
            Opened += OnOpened;
            Closing += OnClosing;
        }

        public SettingsDialog(MountVM mount) : this()
        {
            mount.RAStepsPerDegreeEdit = mount.RAStepsPerDegree;
            mount.DECStepsPerDegreeEdit = mount.DECStepsPerDegree;
            DataContext = mount;
        }

        private void OnOpened(object? sender, EventArgs e)
        {
            if (DataContext is MountVM)
            {
                _dispatchTimer.Start();
            }
        }

        private void OnClosing(object? sender, WindowClosingEventArgs e)
        {
            _dispatchTimer.Stop();
            DataContext = null;
        }

        private void OnTimer(object? sender, EventArgs e)
        {
            if (DataContext is MountVM mount)
            {
                mount.UpdateRealtimeParameters(true);
            }
        }

        private void OnCloseClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
