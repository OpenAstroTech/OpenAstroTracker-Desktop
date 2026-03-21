using Avalonia.Controls;
using OATControl.ViewModels;
using System;

namespace OATControl.Avalonia
{
    public partial class TargetChooser : Window
    {
        public TargetChooser()
        {
            InitializeComponent();
        }

        public TargetChooser(MountVM mount)
        {
            DataContext = mount;
            InitializeComponent();
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            AppSettings.Instance.TargetChooserPos = (Left, Top);
            AppSettings.Instance.TargetChooserSize = (Width, Height);
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (DataContext is MountVM mount)
            {
                mount.TargetChooserClosed();
            }
        }

        public double Left
        {
            get => Position.X;
            set => Position = new global::Avalonia.PixelPoint((int)value, Position.Y);
        }

        public double Top
        {
            get => Position.Y;
            set => Position = new global::Avalonia.PixelPoint(Position.X, (int)value);
        }
    }
}
