using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;
using MahApps.Metro.Controls;
using OATCommuncations.WPF;
using OATCommunications.CommunicationHandlers;
using OATCommunications.Model;
using OATControl.ViewModels;

namespace OATControl
{
	/// <summary>
	/// Interaction logic for DlgWaitForGXState.xaml
	/// </summary>
	public partial class DlgWaitForGXState : MetroWindow, INotifyPropertyChanged
	{
		MountVM _mountVM;
		Action<string, Action<CommandResponse>> _sendCommand;
		Func<string[], bool> _currentStatusFunc;
		DispatcherTimer _timerStatus;
		string _status;
		private DelegateCommand _cancelCommand;
		public event PropertyChangedEventHandler PropertyChanged;

		public DlgWaitForGXState(string status, MountVM mountViewModel, Action<string, Action<CommandResponse>> sendCommand, Func<string[], bool> currentStatusFunc)
		{
			_mountVM = mountViewModel;
			_sendCommand = sendCommand;
			_currentStatusFunc = currentStatusFunc;
			InitializeComponent();
			this.DataContext = this;
			this.Status = status;
			_timerStatus = new DispatcherTimer(TimeSpan.FromMilliseconds(750), DispatcherPriority.Normal, (s, e) => OnTimer(s, e), Application.Current.Dispatcher);
			_timerStatus.Start();
			_cancelCommand = new DelegateCommand(s => OnCancel());
		}

		private void OnCancel()
		{
			_timerStatus.Stop();
			WpfUtilities.RunOnUiThread(() => { this.DialogResult = false; this.Close(); }, Application.Current.Dispatcher);
		}

		public ICommand CancelCommand
		{
			get
			{
				return _cancelCommand;
			}
		}

		private async void OnTimer(object s, EventArgs e)
		{
			_timerStatus.Stop();
			if (_mountVM.ConnectionState.StartsWith("Connected"))
			{
				bool restartTimer = true;
				var gxDoneEvent = new AsyncAutoResetEvent();
				_sendCommand(":GX#,#", (result) =>
				{
					if (result.Success)
					{
						var parts = result.Data.Split(',');
						if (!_currentStatusFunc(parts))
						{
							restartTimer = false;
							WpfUtilities.RunOnUiThread(() => { this.DialogResult = true; this.Close(); }, Application.Current.Dispatcher);
						}
					}
					else
					{
						if (!_currentStatusFunc(null))
						{
							restartTimer = false;
							WpfUtilities.RunOnUiThread(() => { this.DialogResult = true; this.Close(); }, Application.Current.Dispatcher);
						}
					}
					gxDoneEvent.Set();
				});

				await gxDoneEvent.WaitAsync();

				if (restartTimer)
				{
					_timerStatus.Start();
				}
			}
			else
			{
				WpfUtilities.RunOnUiThread(() => { this.DialogResult = true; this.Close(); }, Application.Current.Dispatcher);
			}
		}

		public string Status
		{
			get
			{
				return _status;
			}
			set
			{
				if (value != _status)
				{
					_status = value;
					OnPropertyChanged("Status");
				}
			}
		}

		private void OnPropertyChanged(string field)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(field));
			}
		}
	}
}
