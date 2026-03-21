using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using OATCommunications.Avalonia;
using OATControl.ViewModels;

namespace OATControl.Avalonia
{
	/// <summary>
	/// Interaction logic for DlgMessageBox.axaml
	/// </summary>
	public partial class DlgMessageBox : Window, INotifyPropertyChanged
	{
		private DelegateCommand _closeCommand;
		private string _message = string.Empty;
		public new event PropertyChangedEventHandler? PropertyChanged;

		public DlgMessageBox()
		{
			this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			_closeCommand = new DelegateCommand(s => OnClose());
			InitializeComponent();
			this.DataContext = this;
		}

		public DlgMessageBox(string message)
		{
			this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			_closeCommand = new DelegateCommand(s => OnClose());
			InitializeComponent();
			this.DataContext = this;
			this.Message = message;
		}

		private void OnClose()
		{
			Dispatcher.UIThread.Post(() => { this.Close(true); });
		}

		public DelegateCommand CloseCommand
		{
			get
			{
				return _closeCommand;
			}
		}

		public string Message
		{
			get
			{
				return _message;
			}
			set
			{
				if (value != _message)
				{
					_message = value;
					OnPropertyChanged("Message");
				}
			}
		}

		private void OnPropertyChanged(string field)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(field));
		}
	}
}
