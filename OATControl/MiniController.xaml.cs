using OATControl.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OATControl
{
	/// <summary>
	/// Interaction logic for MiniController.xaml
	/// </summary>
	public partial class MiniController : Window
	{
		MountVM _mount;
		private Point _startCapturePos;
		private Point _startWindowPos;

		public MiniController(MountVM mount)
		{
			_mount = mount;
			this.DataContext = mount;
			InitializeComponent();
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			string cmdParam = string.Empty;
			switch (e.Key)
			{
				case Key.Up: cmdParam = "+N"; break;
				case Key.Down: cmdParam = "+S"; break;
				case Key.Left: cmdParam = "+W"; break;
				case Key.Right: cmdParam = "+E"; break;
			}

			if (!String.IsNullOrEmpty(cmdParam))
			{
				_mount.StartSlewingCommand.Execute(cmdParam);
				e.Handled = true;
			}

			base.OnKeyDown(e);
		}

		protected override void OnKeyUp(KeyEventArgs e)
		{
			string cmdParam = string.Empty;
			switch (e.Key)
			{
				case Key.H: _mount.HomeCommand.Execute(null); e.Handled = true; break;
				case Key.P: _mount.ParkCommand.Execute(null); e.Handled = true; break;
				case Key.Up: cmdParam = "-N"; break;
				case Key.Down: cmdParam = "-S"; break;
				case Key.Left: cmdParam = "-W"; break;
				case Key.Right: cmdParam = "-E"; break;
				case Key.Escape: this.Hide(); break;
			}

			if (!String.IsNullOrEmpty(cmdParam))
			{
				_mount.StartSlewingCommand.Execute(cmdParam);
				e.Handled = true;
			}

			base.OnKeyUp(e);
		}

		private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
		{
			UIElement el = (UIElement)sender;
			if (el.IsEnabled)
			{
				el.CaptureMouse();
				_startCapturePos = PointToScreen(e.GetPosition(el));
				_startWindowPos = new Point(this.Left, this.Top);
				e.Handled = true;
			}
		}

		private void TextBlock_MouseUp(object sender, MouseButtonEventArgs e)
		{
			UIElement el = (UIElement)sender;
			if (el.IsMouseCaptured)
			{
				el.ReleaseMouseCapture();
				e.Handled = true;
			}
		}

		private void TextBlock_MouseMove(object sender, MouseEventArgs e)
		{
			UIElement el = (UIElement)sender;
			if (el.IsMouseCaptured)
			{
				var mousePos = PointToScreen(e.GetPosition(el));
				var delta = new Point(_startCapturePos.X - mousePos.X, _startCapturePos.Y - mousePos.Y);
				this.Left = _startWindowPos.X - delta.X;
				this.Top = _startWindowPos.Y - delta.Y;
				e.Handled = true;
			}
		}
	}
}
