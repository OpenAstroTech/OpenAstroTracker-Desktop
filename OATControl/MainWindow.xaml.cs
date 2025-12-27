using MahApps.Metro.Controls;
using OATControl.ViewModels;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OATControl
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : MetroWindow
	{
		MountVM mountVm;
		public MainWindow()
		{
			mountVm = new MountVM();
			InitializeComponent();
			this.DataContext = mountVm;
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);
			if (AppSettings.Instance.WindowPos.X != -1)
			{
				this.Left = Math.Max(0, Math.Min(AppSettings.Instance.WindowPos.X, System.Windows.SystemParameters.VirtualScreenWidth - 100));
				this.Top = Math.Max(0, Math.Min(AppSettings.Instance.WindowPos.Y, System.Windows.SystemParameters.VirtualScreenHeight- 100));
			}
		}

		protected override void OnContentRendered(EventArgs e)
		{
			base.OnContentRendered(e);
			// Window is visible and ready
			if (this.DataContext is MountVM vm)
			{
				vm.OnAppBooted();
			}
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			mountVm.Disconnect();
			base.OnClosing(e);
			AppSettings.Instance.WindowPos = new Point((int)Math.Max(0, this.Left), (int)Math.Max(0, this.Top));
			AppSettings.Instance.Save();
		}

		private void TextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			TextBox tb = sender as TextBox;
			if (tb != null)
			{
				tb.SelectAll();
				mountVm.SetFocusTarget((string)tb.Tag);
			}
		}

		private void TextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			TextBox tb = sender as TextBox;
			if (tb != null)
			{
				mountVm.SetFocusTarget("");
			}
		}

		private void TextBox_KeyUp(object sender, KeyEventArgs e)
		{
			TextBox tb = sender as TextBox;
			if (tb != null)
			{
				int newNum;
				if (int.TryParse(tb.Text, out newNum))
				{
					string command = tb.Tag as string;
					if (e.Key == Key.Up)
					{
						command += "+";
					}
					else if (e.Key == Key.Down)
					{
						command += "-";
					}
					else
					{
						return;
					}

					mountVm.OnAdjustTarget(command);
				}
			}
		}

		private void OnDecMouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
			mountVm.SetDecLowLimit();
		}
	}
}
