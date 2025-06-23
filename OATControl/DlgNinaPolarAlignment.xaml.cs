using MahApps.Metro.Controls;
using OATCommunications.CommunicationHandlers;
using OATCommunications.Model;
using OATCommunications.WPF;
using OATCommunications.WPF.CommunicationHandlers;
using OATControl.Properties;
using OATControl.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
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
	/// Interaction logic for DlgNinaPoolarAlignment.xaml
	/// </summary>
	public partial class DlgNinaPolarAlignment : MetroWindow, INotifyPropertyChanged
	{
		public class ChecklistItem : INotifyPropertyChanged
		{
			public event PropertyChangedEventHandler PropertyChanged;
			private string _text;
			private bool _isComplete;
			public string Text
			{
				get { return _text; }
				set
				{
					if (_text != value)
					{
						_text = value;
						OnPropertyChanged(nameof(Text));
					}
				}
			}
			public bool IsComplete
			{
				get { return _isComplete; }
				set
				{
					if (_isComplete != value)
					{
						_isComplete = value;
						OnPropertyChanged(nameof(IsComplete));
					}
				}
			}
			protected void OnPropertyChanged(string propertyName)
			{
				if (PropertyChanged != null)
				{
					PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
				}
			}
		}

		private DelegateCommand _closeCommand;
		private string _errorMessage;

		public DlgNinaPolarAlignment(Action closeCallback)
		{
			_closeCommand = new DelegateCommand(() =>
			{
				closeCallback();
			});

			this.DataContext = this;
			InitializeComponent();
		}

		public ObservableCollection<ChecklistItem> ChecklistItems { get; } = new ObservableCollection<ChecklistItem>
		{
			new ChecklistItem { Text = "Plate solved first point..." },
			new ChecklistItem { Text = "Plate solved second point..." },
			new ChecklistItem { Text = "Plate solved third point..." },
			new ChecklistItem { Text = "Calculating error... " },
			new ChecklistItem { Text = "Adjusting mount AZ/ALT..." },
		};

		
		public void SetStatus(string state, string statusDetails)
		{
			switch (state)
			{
				case "Measure":
					if (statusDetails.Contains("First"))
					{
						ChecklistItems[0].IsComplete = true;
					}
					else if (statusDetails.Contains("Second"))
					{
						ChecklistItems[1].IsComplete = true;
					}
					else if (statusDetails.Contains("Third"))
					{
						ChecklistItems[2].IsComplete = true;
					}
					break;
				case "CalculateSettle":
					ChecklistItems[3].IsComplete = true;
					ChecklistItems[3].Text = statusDetails;
					break;
				case "Adjust":
					ChecklistItems[4].IsComplete = true;
					break;
				case "ResetLoop":
					ChecklistItems[3].IsComplete = false;
					ChecklistItems[3].Text = "";
					ChecklistItems[4].IsComplete = false;
					break;
				case "Error":
					ErrorMessage = statusDetails;
					break;
				default:
					break;
			}
		}

		public ICommand CloseCommand { get { return _closeCommand; } }

		public string ErrorMessage
		{
			get { return _errorMessage; }
			set
			{
				if (_errorMessage != value)
				{
					_errorMessage = value;
					OnPropertyChanged(nameof(ErrorMessage));
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