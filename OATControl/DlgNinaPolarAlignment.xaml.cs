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

		private string _firstPointStatus = "InProgress";
		public string FirstPointStatus
		{
			get => _firstPointStatus;
			set
			{
				if (_firstPointStatus != value)
				{
					_firstPointStatus = value;
					OnPropertyChanged(nameof(FirstPointStatus));
				}
			}
		}

		private string _secondPointStatus = "Waiting";
		public string SecondPointStatus
		{
			get => _secondPointStatus;
			set
			{
				if (_secondPointStatus != value)
				{
					_secondPointStatus = value;
					OnPropertyChanged(nameof(SecondPointStatus));
				}
			}
		}

		private string _thirdPointStatus = "Waiting";
		public string ThirdPointStatus
		{
			get => _thirdPointStatus;
			set
			{
				if (_thirdPointStatus != value)
				{
					_thirdPointStatus = value;
					OnPropertyChanged(nameof(ThirdPointStatus));
				}
			}
		}

		private string _calculatingErrorStatus = "Waiting";
		public string CalculatingErrorStatus
		{
			get => _calculatingErrorStatus;
			set
			{
				if (_calculatingErrorStatus != value)
				{
					_calculatingErrorStatus = value;
					OnPropertyChanged(nameof(CalculatingErrorStatus));
				}
			}
		}

		private string _adjustingMountStatus = "Waiting";
		public string AdjustingMountStatus
		{
			get => _adjustingMountStatus;
			set
			{
				if (_adjustingMountStatus != value)
				{
					_adjustingMountStatus = value;
					OnPropertyChanged(nameof(AdjustingMountStatus));
				}
			}
		}

		private string _azimuthError = "-";
		public string AzimuthError
		{
			get => _azimuthError;
			set
			{
				if (_azimuthError != value)
				{
					_azimuthError = value;
					OnPropertyChanged(nameof(AzimuthError));
				}
			}
		}
		
		private string _altitudeError = "-";
		public string AltitudeError
		{
			get => _altitudeError;
			set
			{
				if (_altitudeError != value)
				{
					_altitudeError = value;
					OnPropertyChanged(nameof(AltitudeError));
				}
			}
		}
		private string _totalError = "-";
		public string TotalError
		{
			get => _totalError;
			set
			{
				if (_totalError != value)
				{
					_totalError = value;
					OnPropertyChanged(nameof(TotalError));
				}
			}
		}

		private string _iterations= "";
		public string Iterations
		{
			get => _iterations;
			set
			{
				if (_iterations != value)
				{
					_iterations = value;
					OnPropertyChanged(nameof(Iterations));
				}
			}
		}

		public void SetStatus(string state, string statusDetails)
		{
			switch (state)
			{
				case "Measure":
					if (statusDetails.Contains("First"))
					{
						FirstPointStatus = "Complete";
						SecondPointStatus = "InProgress";
					}
					else if (statusDetails.Contains("Second"))
					{
						SecondPointStatus = "Complete";
						ThirdPointStatus = "InProgress";
					}
					else if (statusDetails.Contains("Third"))
					{
						ThirdPointStatus = "Complete";
						CalculatingErrorStatus = "InProgress";
					}
					break;
				case "CalculateSettle":
					AzimuthError = statusDetails.Split('|')[0];
					AltitudeError = statusDetails.Split('|')[1];
					TotalError = statusDetails.Split('|')[2];
					Iterations = statusDetails.Split('|')[3];
					break;
				case "Adjust":
					CalculatingErrorStatus = "Complete";
					AdjustingMountStatus = "InProgress";
					break;
				case "ResetLoop":
					CalculatingErrorStatus = "InProgress";
					AzimuthError = "-";
					AltitudeError = "-";
					Iterations = "";
					AdjustingMountStatus = "Waiting";
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