﻿using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Model.Scene;
using System.Windows;

using OATSimulation.ViewModels;
using System.Windows.Media;

namespace OATSimulation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainViewModel(this);

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
    }
}
