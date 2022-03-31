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
using MahApps.Metro.Controls;
using OATCommunications.WPF;
using OATControl.ViewModels;

namespace OATControl
{
	/// <summary>
	/// Interaction logic for DlgCustomActionSetup.xaml
	/// </summary>
	public partial class DlgCustomActionSetup : Window, INotifyPropertyChanged
	{
		private DelegateCommand _okCommand;
		private DelegateCommand _closeCommand;

		public DlgCustomActionSetup()
		{
			_closeCommand = new DelegateCommand(() =>
			{
				this.DialogResult = false;
				this.Close();
			});

			_okCommand = new DelegateCommand(() =>
			{
				this.DialogResult = true;
				this.Close();
			});

			this.DataContext = this;
			InitializeComponent();

		}

		public ICommand OKCommand { get { return _okCommand; } }
		public ICommand CloseCommand { get { return _closeCommand; } }


		/// <summary>
		/// Gets or sets the ButtonText
		/// </summary>
		string _buttonText = string.Empty;
		public string ButtonText
		{
			get { return _buttonText; }
			set
			{
				if (_buttonText != value)
				{
					_buttonText = value;
					OnPropertyChanged("ButtonText");
				}
			}
		}

		/// <summary>
		/// Gets or sets the CustomCommand
		/// </summary>
		string _commandText = string.Empty;
		public string CommandText
		{
			get { return _commandText; }
			set
			{
				if (_commandText != value)
				{
					_commandText = value;
					OnPropertyChanged("CommandText");
				}
			}
		}



		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged(string field)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(field));
			}
		}

	}
}
