using System;
using Avalonia.Controls;
using Avalonia.Threading;
using System.Threading.Tasks;

namespace OATTest
{
    public partial class MainWindow : Window
    {
        TestVM _vm;

        public MainWindow()
        {
            _vm = new TestVM();
            _vm.ConfirmAction = ShowConfirmDialog;
            DataContext = _vm;
            InitializeComponent();

            // Auto-scroll test list when SelectedTest changes
            _vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(_vm.SelectedTest))
                    ScrollIntoView(TestListBox, _vm.SelectedTest);
                else if (e.PropertyName == "LastLineIndex")
                    ScrollToEnd(DebugListBox);
            };
        }

        protected override async void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            await _vm.OnDiscoverDevices();
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            _vm.Shutdown();
            base.OnClosing(e);
        }

        private async Task<bool> ShowConfirmDialog(string message)
        {
            var dialog = new Window
            {
                Title = "Pre-run warning",
                Width = 400,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(16),
                    Spacing = 12,
                    Children =
                    {
                        new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                        new StackPanel
                        {
                            Orientation = Avalonia.Layout.Orientation.Horizontal,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                            Spacing = 8,
                            Children =
                            {
                                new Button { Content = "Yes", Tag = true },
                                new Button { Content = "No",  Tag = false }
                            }
                        }
                    }
                }
            };

            bool result = false;
            var panel = (StackPanel)((StackPanel)dialog.Content).Children[1];
            foreach (Button btn in panel.Children)
            {
                btn.Click += (_, _) => { result = (bool)btn.Tag!; dialog.Close(); };
            }

            await dialog.ShowDialog(this);
            return result;
        }

        private void ScrollIntoView(ListBox listBox, object? item)
        {
            if (item == null) return;
            Dispatcher.UIThread.Post(() => listBox.ScrollIntoView(item));
        }

        private void ScrollToEnd(ListBox listBox)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (listBox.ItemCount > 0)
                    listBox.ScrollIntoView(listBox.ItemCount - 1);
            });
        }
    }
}
