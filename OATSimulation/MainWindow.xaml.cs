using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Model.Scene;
using System.Windows;

using OATSimulation.ViewModels;
using System.Windows.Media;
using System.Windows.Input;
using System;
using System.ComponentModel;

namespace OATSimulation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel = null;
        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel(this);
            DataContext = _viewModel;

            // Font
            this.FontFamily = new FontFamily("Courier New");

            view.AddHandler(Element3D.MouseDown3DEvent, new RoutedEventHandler((s, e) =>
            {
                var arg = e as MouseDown3DEventArgs;

                if (arg.HitTestResult == null)
                {
                    return;
                }
                if (arg.HitTestResult.ModelHit is SceneNode node && node.Tag is AttachedNodeViewModel vm)
                {
                    vm.Selected = !vm.Selected;
                }
            }));
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            _viewModel.OnClosing();

            AppSettings.Instance.WindowPos = new Point((int)Math.Max(0, this.Left), (int)Math.Max(0, this.Top));
            AppSettings.Instance.Save();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.D1:
                    _viewModel.LightPresetNumber = 1;
                    e.Handled = true;
                    break;
                case Key.D2:
                    _viewModel.LightPresetNumber = 2;
                    e.Handled = true;
                    break;
            }

            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            string cmdParam = string.Empty;
            switch (e.Key)
            {
                case Key.H:
                    e.Handled = true;
                    break;
            }

            if (!string.IsNullOrEmpty(cmdParam))
            {
                e.Handled = true;
            }

            base.OnKeyUp(e);
        }
    }
}
