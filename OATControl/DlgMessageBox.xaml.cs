using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using OATCommuncations.WPF;
using OATControl.ViewModels;

namespace OATControl
{
	/// <summary>
	/// Interaction logic for DlgWaitForGXState.xaml
	/// </summary>
	public partial class DlgMessageBox: Window, INotifyPropertyChanged
	{
		private DelegateCommand _closeCommand;
		private string _message;
		public event PropertyChangedEventHandler PropertyChanged;

		public DlgMessageBox(string message)
		{
			this.Owner = Application.Current.MainWindow;
			this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			InitializeComponent();
			this.DataContext = this;
			this.Message = message;
			_closeCommand = new DelegateCommand(s => OnClose());
		}

		private void OnClose()
		{
			WpfUtilities.RunOnUiThread(() => { this.DialogResult = false; this.Close(); }, Application.Current.Dispatcher);
		}

		public ICommand CloseCommand
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
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(field));
			}
		}
	}
}
