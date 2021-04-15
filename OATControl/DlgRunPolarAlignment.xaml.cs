using MahApps.Metro.Controls;
using OATCommunications.CommunicationHandlers;
using OATCommunications.Model;
using OATCommunications.WPF.CommunicationHandlers;
using OATControl.Properties;
using OATControl.ViewModels;
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
using System.Windows.Shapes;

namespace OATControl
{
	/// <summary>
	/// Interaction logic for DlgChooseOat.xaml
	/// </summary>
	public partial class DlgRunPolarAlignment : MetroWindow, INotifyPropertyChanged
	{
		private DelegateCommand _okCommand;
		private DelegateCommand _closeCommand;
		private int _state;
		private Action<string, Action<CommandResponse>> _sendCommand;

		public DlgRunPolarAlignment(Action<string, Action<CommandResponse>> sendCommand)
		{
			_sendCommand = sendCommand;
			_closeCommand = new DelegateCommand(() =>
			{
				this.DialogResult = false;
				this.Close();
			});

			_okCommand = new DelegateCommand(async () =>
			{
				this.State++;
				if (_state == 2)
				{
					// Move RA to Polaris
					await SendCommandAsync($":Sr02:59:09#,n");

					// Move DEC to twice Polaris Dec
					if (Settings.Default.SiteLatitude > 0) // Northern hemisphere
					{
						await SendCommandAsync($":Sd+88*42:12#,n");
					}
					else
					{
						await SendCommandAsync($":Sd-88*42:12#,n");
					}
					await SendCommandAsync($":MS#,n");
				}
				else if (_state == 3)
				{
					// Sync the mount to Polaris coordinates
					if (Settings.Default.SiteLatitude > 0) // Northern hemisphere
					{
						await SendCommandAsync($":SY+89*21:06.02:59:09#,n");
					}
					else
					{
						await SendCommandAsync($":SY-89*21:06.02:59:09#,n");
					}
				}
			});

			this.DataContext = this;
			State = 1;
			InitializeComponent();
		}

		private async Task<bool> SendCommandAsync(string command)
		{
			var doneEvent = new AsyncAutoResetEvent();
			var success = false;
			_sendCommand(command, (e) =>
			{
				success = e.Success;
				doneEvent.Set();
			});
			await doneEvent.WaitAsync();
			return success;
		}

		public ICommand OKCommand { get { return _okCommand; } }
		public ICommand CloseCommand { get { return _closeCommand; } }

		public event PropertyChangedEventHandler PropertyChanged;

		public int State
		{
			get { return _state; }
			set
			{
				if (value != _state)
				{
					_state = value;
					OnPropertyChanged("State");
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