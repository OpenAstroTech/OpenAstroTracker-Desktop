using OATCommunications.Utilities;
using OATControl.Properties;
using OATControl.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
		private string _lastCommand = string.Empty;

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
				case Key.W: cmdParam = _mount.ScopeHasALT ? "+A" : ""; break;
				case Key.S: cmdParam = _mount.ScopeHasALT ? "+Z" : ""; break;
				case Key.A: cmdParam = _mount.ScopeHasAZ ? "+L" : ""; break;
				case Key.D: cmdParam = _mount.ScopeHasAZ ? "+R" : ""; break;
				case Key.X: cmdParam = _mount.ScopeHasFOC ? "+F" : ""; break;
				case Key.C: cmdParam = _mount.ScopeHasFOC ? "+G" : ""; break;
			}

			if (!String.IsNullOrEmpty(cmdParam))
			{
				if (_lastCommand != cmdParam)
				{
					Log.WriteLine("MiniCtrl: KeyDown - Send command " + cmdParam);
					_mount.ChangeSlewingStateCommand.Execute(cmdParam);
					_lastCommand = cmdParam;
				}
				e.Handled = true;
			}

			base.OnKeyDown(e);
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			AppSettings.Instance.MiniControllerPos = new Point((int)this.Left, (int)this.Top);
			base.OnClosing(e);
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
				case Key.W: cmdParam = _mount.ScopeHasALT ? "-A" : ""; break;
				case Key.S: cmdParam = _mount.ScopeHasALT ? "-Z" : ""; break;
				case Key.A: cmdParam = _mount.ScopeHasAZ ? "-L" : ""; break;
				case Key.D: cmdParam = _mount.ScopeHasAZ ? "-R" : ""; break;
				case Key.X: cmdParam = _mount.ScopeHasFOC ? "-F" : ""; break;
				case Key.C: cmdParam = _mount.ScopeHasFOC ? "-G" : ""; break;
				case Key.D1: _mount.SlewRate = 1; break;
				case Key.D2: _mount.SlewRate = 2; break;
				case Key.D3: _mount.SlewRate = 3; break;
				case Key.D4: _mount.SlewRate = 4; break;
				case Key.D5: _mount.SlewRate = 5; break;
				case Key.Escape: this.Hide(); break;
			}

			if (!String.IsNullOrEmpty(cmdParam))
			{
				Log.WriteLine("MiniCtrl: KeyUp - Send command " + cmdParam);
				_mount.ChangeSlewingStateCommand.Execute(cmdParam);
				_lastCommand = string.Empty;
				e.Handled = true;
			}

			base.OnKeyUp(e);
		}

		private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				DragMove();
			}
		}
	}
}
