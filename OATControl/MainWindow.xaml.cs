using MahApps.Metro.Controls;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

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
