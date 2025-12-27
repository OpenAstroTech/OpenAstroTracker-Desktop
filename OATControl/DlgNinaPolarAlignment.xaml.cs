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
using System.Windows.Threading;

namespace OATControl
{
	public class ErrorEntry
	{
		public string AzimuthError { get; set; }
		public string AltitudeError { get; set; }
		public string TotalError { get; set; }
	}

	/// <summary>
	/// Interaction logic for DlgNinaPoolarAlignment.xaml
	/// </summary>
	public partial class DlgNinaPolarAlignment : MetroWindow, INotifyPropertyChanged, IPolarAlignDialog
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
		private ObservableCollection<ErrorEntry> _errorEntries = new ObservableCollection<ErrorEntry>();
		public ObservableCollection<ErrorEntry> ErrorEntries => _errorEntries;

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
		
		private string _timeLeft = "";
		public string TimeLeft
		{
			get => _timeLeft;
			set
			{
				if (_timeLeft != value)
				{
					_timeLeft = value;
					OnPropertyChanged(nameof(TimeLeft));
				}
			}
		}

		private int _activeLine = 0;
		public int ActiveLine
		{
			get => _activeLine;
			set
			{
				if (_activeLine != value)
				{
					_activeLine = value;
					OnPropertyChanged(nameof(ActiveLine));
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
						ActiveLine = 2;
						FirstPointStatus = "Complete";
						SecondPointStatus = "InProgress";
					}
					else if (statusDetails.Contains("Second"))
					{
						ActiveLine = 3;
						SecondPointStatus = "Complete";
						ThirdPointStatus = "InProgress";
					}
					else if (statusDetails.Contains("Third"))
					{
						ThirdPointStatus = "Complete";
						ActiveLine = 4;
						CalculatingErrorStatus = "InProgress";
					}
					break;
				case "CalculateSettle":
					var parts = statusDetails.Split('|');
					if (parts.Length >= 4)
					{
						if (parts[3] == "(2/2)")
						{
							_errorEntries.RemoveAt(_errorEntries.Count-1);
						}
						_errorEntries.Add(new ErrorEntry
						{
							AzimuthError = parts[0],
							AltitudeError = parts[1],
							TotalError = parts[2],
						});
						// Limit to at most 4 entries
						if (_errorEntries.Count > 4)
						{
							_errorEntries.RemoveAt(0);
						}
					}
					
					Iterations = parts[3];
					break;
				case "Adjust":
					CalculatingErrorStatus = "Complete";
					ActiveLine = 5;
					AdjustingMountStatus = "InProgress";
					break;
				case "ResetLoop":
					ActiveLine = 4;
					CalculatingErrorStatus = "InProgress";
					AzimuthError = "-";
					AltitudeError = "-";
					Iterations = "";
					AdjustingMountStatus = "Waiting";
					break;
				case "Error":
					ErrorMessage = statusDetails;
					break;
				case "Succeeded":
					CalculatingErrorStatus = "Complete";
					AdjustingMountStatus = "Complete";
					ActiveLine = 0;
					ErrorMessage = statusDetails;
					var _closeTime = DateTime.UtcNow + TimeSpan.FromSeconds(5);
					var timer = new DispatcherTimer();
					timer.Interval = TimeSpan.FromSeconds(0.1); // Set your delay here
					timer.Tick += (s, e) =>
					{
						if (DateTime.UtcNow < _closeTime)
						{
							TimeLeft = $"({((_closeTime - DateTime.UtcNow).TotalSeconds+1).ToString("F0")})";
							return; // Still within the delay period
						}
						timer.Stop(); // Stop the timer so it only runs once

						// Your code to execute after the delay
						_closeCommand.Execute(null);
					};
					timer.Start();
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