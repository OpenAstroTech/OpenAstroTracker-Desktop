using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Model.Scene;
using System.Windows;

using OATSimulation.ViewModels;
using System.Windows.Media;
using System.Windows.Input;
using System;

namespace OATSimulation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainViewModel viewModel = null;
        public MainWindow()
        {
            InitializeComponent();
            viewModel = new MainViewModel(this);
            DataContext = viewModel;

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

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.D1:
                    viewModel.ToggleScenePresets(1);
                    e.Handled = true;
                    break;
                case Key.D2:
                    viewModel.ToggleScenePresets(2);
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
