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
using System.Windows.Threading;

namespace OATControl
{
	/// <summary>
	/// Interaction logic for SettingsDialog.xaml
	/// </summary>
	public partial class SettingsDialog : Window
	{
		DispatcherTimer dispatchTimer;
		MountVM _mount;
		public SettingsDialog(MountVM mount)
		{
			dispatchTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(333), DispatcherPriority.Normal, OnTimer, Dispatcher.CurrentDispatcher);
			_mount = mount;
			this.DataContext = _mount;
			_mount.RAStepsPerDegreeEdit = _mount.RAStepsPerDegree;
			_mount.DECStepsPerDegreeEdit = _mount.DECStepsPerDegree;
			InitializeComponent();
		}

		private void OnTimer(object sender, EventArgs e)
		{
			dispatchTimer.Stop();
			if (this.DataContext != null)
			{
				(this.DataContext as MountVM).UpdateRealtimeParameters(true);
				dispatchTimer.Start();
			}
		}
		protected override void OnClosing(CancelEventArgs e)
		{
			dispatchTimer.Stop();
			this.DataContext = null;
			base.OnClosing(e);
		}
	}
}
