using MahApps.Metro.Controls;
using OATCommunications.Model;
using OATCommunications.Utilities;
using OATCommunications.WPF;
using OATControl.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
	/// <summary>
	/// Interaction logic for DlgStepCalibration.xaml
	/// </summary>
	public partial class DlgAxisCalibration : MetroWindow, INotifyPropertyChanged
	{
		enum CalibrationState
		{
			WaitToStart = 1,
			WaitForStartAxisSolution = 2,
			WaitForEndAxisSolution = 3,
			ConfirmResults = 4
		}
		private MountVM _mountVM;
		private string _selectedAxis;
		private char _axisChar;
		private float _axisStepsBefore;
		private float _calculatedStepsPerDegree;
		private float _mountStepsPerDegree;
		private float _errorRatio;

		private float _axisStepsAfter;

		public float _raSolvedStart;
		public float _decSolvedStart;
		public float _raSolvedEnd;
		public float _decSolvedEnd;
		private string _inputCoordinateRA;
		private string _inputCoordinateDEC;
		private string _leftSlew;
		private string _rightSlew;
		private string _stepsMoved;
		private string _suggestion;
		private string _azAltWarning;
		public float _axisSolvedEnd;

		private CalibrationState _calibrationState;
		DelegateCommand _transferStepsToMountCommand;
		DelegateCommand _changeSlewingStateCommand;
		DelegateCommand _cancelCommand;
		DelegateCommand _continueCommand;
		DelegateCommand _closeCommand;
		private bool _displayStatus;
		private bool _canContinue = true;
		private DispatcherTimer _timer;

		public event PropertyChangedEventHandler PropertyChanged;

		public DlgAxisCalibration(MountVM mountVM)
		{
			_mountVM = mountVM;
			_mountVM.PlateSolveOccurred += this.PlateSolveOccurred;

			_calibrationState = CalibrationState.WaitToStart;
			_displayStatus = false;

			_cancelCommand = new DelegateCommand(() => OnCancel(), () => true);
			_continueCommand = new DelegateCommand(() => OnContinue(), () => true);
			_closeCommand = new DelegateCommand(() => OnClose(), () => true);
			_changeSlewingStateCommand = new DelegateCommand((arg) => OnChangeSlewingState((string)arg), () => true);
			_transferStepsToMountCommand = new DelegateCommand(async (arg) => await OnTransferStepsToMount((string)arg), () => true);

			_timer = new DispatcherTimer(TimeSpan.FromSeconds(1.0), DispatcherPriority.Normal, OnTimerTick, Application.Current.Dispatcher);

			this.DataContext = this;
			InitializeComponent();
			if (_mountVM.FirmwareVersion <= 11316)
			{
				AzAltWarning = "ALT/AZ axis calibration is only supported on firmware version 1.13.17 or later.";
			}
			SelectedAxis = "RA";
		}

		private void PlateSolveOccurred(object sender, PlatesolveEventArgs e)
		{
			WpfUtilities.RunOnUiThread(() =>
			{
				var curRA = new DayTime(e.RAHours);
				var curDEC = new Declination(e.DecDegrees);
				if ((_calibrationState == CalibrationState.WaitForStartAxisSolution) || (_calibrationState == CalibrationState.WaitForEndAxisSolution))
				{
					InputCoordinateRA = curRA.ToUIString();
					InputCoordinateDEC = curDEC.ToUIString();
					Log.WriteLine($"AXISCALIB: Received Platesolve results. RA: {curRA.ToUIString()}  DEC:{curDEC.ToUIString()}");
				}
			}, Application.Current.Dispatcher);
		}

		public ICommand ChangeSlewingStateCommand { get { return _changeSlewingStateCommand; } }
		public ICommand TransferStepsToMountCommand { get { return _transferStepsToMountCommand; } }
		public ICommand CancelCommand { get { return _cancelCommand; } }
		public ICommand ContinueCommand { get { return _continueCommand; } }
		public ICommand CloseCommand { get { return _closeCommand; } }

		private void OnPropertyChanged([CallerMemberName] string field = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(field));
				// RequeryCommands();
			}
		}

		public void OnCancel()
		{
			this.Close();
		}

		public void OnClose()
		{
			_mountVM.PlateSolveOccurred -= this.PlateSolveOccurred;
			this.Close();
		}

		public string AzAltWarning
		{
			get { return _azAltWarning; }
			set
			{
				if (value != _azAltWarning)
				{
					_azAltWarning = value;
					OnPropertyChanged();
				}
			}
		}

		public string InputCoordinateRA
		{
			get { return _inputCoordinateRA; }
			set
			{
				if (value != _inputCoordinateRA)
				{
					_inputCoordinateRA = value;
					OnPropertyChanged();
				}
			}
		}

		public string InputCoordinateDEC
		{
			get { return _inputCoordinateDEC; }
			set
			{
				if (value != _inputCoordinateDEC)
				{
					_inputCoordinateDEC = value;
					OnPropertyChanged();
				}
			}
		}

		void OnChangeSlewingState(string arg)
		{
			_mountVM.ChangeSlewingStateCommand.Execute(arg);
		}

		async Task OnTransferStepsToMount(string arg)
		{
			switch (arg)
			{
				case "RA":
					Log.WriteLine($"AXISCALIB: Updating RA steps/deg to {CalculatedStepsPerDegree.ToString("F1")}");
					await _mountVM.SetSteps(CalculatedStepsPerDegree, _mountVM.DECStepsPerDegree);
					break;
				case "DEC":
					Log.WriteLine($"AXISCALIB: Updating DEC steps/deg to {CalculatedStepsPerDegree.ToString("F1")}");
					await _mountVM.SetSteps(_mountVM.RAStepsPerDegree, CalculatedStepsPerDegree);
					break;
				case "ALT":
					Log.WriteLine($"AXISCALIB: Updating ALT steps/deg to {CalculatedStepsPerDegree.ToString("F1")}");
					await _mountVM.SetSecondarySteps(_mountVM.AZStepsPerDegree, CalculatedStepsPerDegree);
					break;
				case "AZ":
					Log.WriteLine($"AXISCALIB: Updating AZ steps/deg to {CalculatedStepsPerDegree.ToString("F1")}");
					await _mountVM.SetSecondarySteps(CalculatedStepsPerDegree, _mountVM.AZStepsPerDegree);
					break;
			}
		}

		public string StepsMoved
		{
			get { return _stepsMoved; }
			set
			{
				if (value != _stepsMoved)
				{
					_stepsMoved = value;
					OnPropertyChanged();
				}
			}
		}

		public string Suggestion
		{
			get { return _suggestion; }
			set
			{
				if (value != _suggestion)
				{
					_suggestion = value;
					OnPropertyChanged();
				}
			}
		}

		public bool IsPrimaryAxis
		{
			get { return (_selectedAxis == "RA") || (_selectedAxis == "DEC"); }
		}

		public float AxisStepsBefore
		{
			get { return _axisStepsBefore; }
			set
			{
				if (value != _axisStepsBefore)
				{
					_axisStepsBefore = value;
					OnPropertyChanged();
				}
			}
		}

		public string LeftSlew
		{
			get { return _leftSlew; }
			set
			{
				if (value != _leftSlew)
				{
					_leftSlew = value;
					OnPropertyChanged();
				}
			}
		}

		public string RightSlew
		{
			get { return _rightSlew; }
			set
			{
				if (value != _rightSlew)
				{
					_rightSlew = value;
					OnPropertyChanged();
				}
			}
		}


		public float AxisStepsAfter
		{
			get { return _axisStepsAfter; }
			set
			{
				if (value != _axisStepsAfter)
				{
					_axisStepsAfter = value;
					OnPropertyChanged();
				}
			}
		}


		public float ErrorRatio
		{
			get
			{
				return _errorRatio;
			}
			set
			{
				if (value != _errorRatio)
				{
					_errorRatio = value;
					OnPropertyChanged();
				}
			}
		}

		public float CalculatedStepsPerDegree
		{
			get
			{
				return _calculatedStepsPerDegree;
			}
			set
			{
				if (value != _calculatedStepsPerDegree)
				{
					_calculatedStepsPerDegree = value;
					OnPropertyChanged();
				}
			}
		}

		public float MountStepsPerDegree
		{
			get
			{
				return _mountStepsPerDegree;
			}
			set
			{
				if (value != _mountStepsPerDegree)
				{
					_mountStepsPerDegree = value;
					OnPropertyChanged();
				}
			}
		}



		public bool DisplayStatus
		{
			get { return _displayStatus; }
			set
			{
				if (value != _displayStatus)
				{
					_displayStatus = value;
					OnPropertyChanged();
				}
			}
		}

		public bool CanContinue
		{
			get { return _canContinue; }
			set
			{
				if (value != _canContinue)
				{
					_canContinue = value;
					OnPropertyChanged();
				}
			}
		}

		public int Step
		{
			get { return (int)_calibrationState; }
		}

		public IEnumerable<String> AvailableAxis
		{
			get
			{
				yield return "RA";
				yield return "DEC";
				if (_mountVM.ScopeHasALT && _mountVM.FirmwareVersion > 11316)
				{
					yield return "ALT";
				}
				if (_mountVM.ScopeHasAZ && _mountVM.FirmwareVersion > 11316)
				{
					yield return "AZ";
				}
			}
		}

		public int SlewRate
		{
			get { return _mountVM.SlewRate; }
			set
			{
				if (value != _mountVM.SlewRate)
				{
					_mountVM.SlewRate = value;
					OnPropertyChanged();
				}
			}
		}


		public String SelectedAxis
		{
			get { return _selectedAxis; }
			set
			{
				if (_selectedAxis != value)
				{
					_selectedAxis = value;
					switch (value)
					{
						case "RA": 
							_axisChar = 'r'; 
							LeftSlew = "W"; 
							RightSlew = "E";
							AzAltWarning = "Start DEC below 75° and slew RA by at least 15° when prompted.";
							if (_mountVM.FirmwareVersion <= 11316)
							{
								AzAltWarning += "\nALT/AZ axis calibration is only supported on firmware version V1.13.17 or later.";
							}
							break;
						case "DEC":
							_axisChar = 'd'; 
							LeftSlew = "S"; 
							RightSlew = "N";
							AzAltWarning = "Start DEC below 80° and slew to at least 65°, without crossing\nthe home point (celestial pole).";
							if (_mountVM.FirmwareVersion <= 11316)
							{
								AzAltWarning += "\nALT/AZ axis calibration is only supported on firmware version V1.13.17 or later.";
							}
							break;
						case "ALT":
							_axisChar = 'l'; 
							LeftSlew = "A"; 
							RightSlew = "Z";
							AzAltWarning = "Ensure mount is as level as possible.";
							break;
						case "AZ":
							_axisChar = 'z'; 
							LeftSlew = "L"; 
							RightSlew = "R";
							AzAltWarning = "Ensure mount is as level as possible.";
							break;
					}
					AzAltWarning += "\nYou can use MiniControl now to move mount as needed before starting.";
					OnPropertyChanged();
					OnPropertyChanged("IsPrimaryAxis");
					AxisStepsBefore = GetAxisPosition(_selectedAxis);
				}
			}
		}

		private float GetAxisPosition(string axis)
		{
			switch (axis)
			{
				case "RA": return _mountVM.RAStepper;
				case "DEC": return _mountVM.DECStepper;
				case "ALT": return _mountVM.ALTStepper;
				case "AZ": return _mountVM.AZStepper;
				default: return 0;
			}
		}

		private void OnTimerTick(object sender, EventArgs e)
		{
			switch (_calibrationState)
			{
				case CalibrationState.WaitToStart:
					AxisStepsBefore = GetAxisPosition(_selectedAxis);
					break;

				case CalibrationState.WaitForStartAxisSolution:
					CanContinue = MountVM.TryParseCoord(InputCoordinateRA, out _raSolvedStart);
					CanContinue &= MountVM.TryParseCoord(InputCoordinateDEC, out _decSolvedStart);
					break;

				case CalibrationState.WaitForEndAxisSolution:
					var currentSteps = GetAxisPosition(_selectedAxis);
					StepsMoved = (currentSteps - _axisStepsBefore).ToString();
					CanContinue = Math.Abs(currentSteps - _axisStepsBefore) > 10;
					CanContinue &= MountVM.TryParseCoord(InputCoordinateRA, out _raSolvedEnd);
					CanContinue &= MountVM.TryParseCoord(InputCoordinateDEC, out _decSolvedEnd);
					break;
			}
		}

		private void GenerateSuggestion(string axis, float movedDegrees, float stepsMoved)
		{
			if ((ErrorRatio < 53) || (ErrorRatio > 147))
			{
				Suggestion = string.Format("The actual steps are a factor of more than 2 different from the configured steps, likely due to an incorrect config file, or stepper setting changes without a Facory Reset. Do not use Set to use this value.");
			}
			else if ((ErrorRatio < 95) || (ErrorRatio > 105))
			{
				Suggestion = string.Format("The actual steps differ by a lot from the configured steps. You should set the steps to the calculated value.");
			}
			else if ((ErrorRatio < 97.5) || (ErrorRatio > 102.5))
			{
				Suggestion = string.Format("The actual steps differ a fair amount from the configured steps. You should set the steps to the calculated value.");
			}
			else if ((ErrorRatio < 99.4) || (ErrorRatio > 100.6))
			{
				Suggestion = "The actual steps are pretty close to the configured steps, make the adjustment if your goto is more than a degree.";
			}
			else
			{
				Suggestion = "This axis is very well calibrated, no step adjustment is needed.";
			}
			if (Math.Sign(movedDegrees) != Math.Sign(stepsMoved))
			{
				Suggestion += $"\nIt seems your {axis} axis is inverted. You should add\n      #define {axis}_INVERT_DIR 1\nto your local config (or set it to 0 if it already there).";
			}
		}

		public void OnContinue()
		{
			switch (_calibrationState)
			{
				case CalibrationState.WaitToStart:
					_calibrationState = CalibrationState.WaitForStartAxisSolution;
					OnPropertyChanged("Step");
					CanContinue = false;
					Log.WriteLine($"AXISCALIB: Starting {_selectedAxis} calibration. Initial position: {_axisStepsBefore.ToString("F0")}");
					_timer.Start();
					break;

				case CalibrationState.WaitForStartAxisSolution:
					_calibrationState = CalibrationState.WaitForEndAxisSolution;
					Log.WriteLine($"AXISCALIB: First solution is RA:{InputCoordinateRA} and DEC:{InputCoordinateDEC}");
					InputCoordinateDEC = "";
					InputCoordinateRA = "";
					OnPropertyChanged("Step");
					CanContinue = false;
					break;

				case CalibrationState.WaitForEndAxisSolution:
					var currentSteps = GetAxisPosition(_selectedAxis);
					float stepsMoved = currentSteps - _axisStepsBefore;
					MountVM.TryParseCoord(InputCoordinateRA, out _raSolvedEnd);
					MountVM.TryParseCoord(InputCoordinateDEC, out _decSolvedEnd);
					Log.WriteLine($"AXISCALIB: Second solution is RA:{InputCoordinateRA} ({_raSolvedEnd.ToString("F5")}) and DEC:{InputCoordinateDEC} ({_decSolvedEnd.ToString("F5")}). StepsMoved:{stepsMoved}");
					switch (SelectedAxis)
					{
						case "RA":
							{
								float movedDegrees = 15.0f * (_raSolvedStart - _raSolvedEnd);
								CalculatedStepsPerDegree = Math.Abs(stepsMoved / movedDegrees);
								MountStepsPerDegree = _mountVM.RAStepsPerDegree;
								ErrorRatio = 100.0f * CalculatedStepsPerDegree / MountStepsPerDegree;
								GenerateSuggestion("RA", movedDegrees, stepsMoved);
								Log.WriteLine($"AXISCALIB: RA calibration. MovedDegrees:{movedDegrees.ToString("F2")}. Calculated steps/deg:({CalculatedStepsPerDegree.ToString("F1")}). Mount steps/deg:{MountStepsPerDegree.ToString("F1")}");
								Log.WriteLine($"AXISCALIB: RA calibration error:{ErrorRatio.ToString("F1")}%");
							}
							break;

						case "DEC":
							{
								// The direction of the stepper deltas changes on either side of home.
								float movedDegrees = (_decSolvedStart - _decSolvedEnd) * Math.Sign(currentSteps);
								float movedRADegrees = Math.Abs(15.0f * (_raSolvedStart - _raSolvedEnd));
								if ((movedRADegrees > 175) && (movedRADegrees < 185))
								{
									movedDegrees = (float)((90.0f - _decSolvedEnd) + (90.0f - _decSolvedStart)) * Math.Sign(movedDegrees);
								}
								CalculatedStepsPerDegree = Math.Abs(stepsMoved / movedDegrees);
								MountStepsPerDegree = _mountVM.DECStepsPerDegree;
								ErrorRatio = 100.0f * CalculatedStepsPerDegree / MountStepsPerDegree;
								GenerateSuggestion("DEC", movedDegrees, stepsMoved);
								Log.WriteLine($"AXISCALIB: DEC calibration. DecSolvedStart:{_decSolvedStart.ToString("F2")}. DecSolvedEnd:{_decSolvedEnd.ToString("F2")}. Currentsteps:{currentSteps}");
								Log.WriteLine($"AXISCALIB: DEC calibration. MovedRADegrees:{movedRADegrees.ToString("F2")}");
								Log.WriteLine($"AXISCALIB: DEC calibration. MovedDegrees:{movedDegrees.ToString("F2")}. Calculated steps/deg:({CalculatedStepsPerDegree.ToString("F1")}). Mount steps/deg:{MountStepsPerDegree.ToString("F1")}");
								Log.WriteLine($"AXISCALIB: DEC calibration error:{ErrorRatio.ToString("F1")}%");
							}
							break;
						case "ALT":
						case "AZ":
							{
								Log.WriteLine($"AXISCALIB: ALT/AZ calibration. DecSolvedStart:{_decSolvedStart.ToString("F5")}. DecSolvedEnd:{_decSolvedEnd.ToString("F5")}");
								Log.WriteLine($"AXISCALIB: ALT/AZ calibration. RaSolvedStart:{_raSolvedStart.ToString("F5")}. RaSolvedEnd:{_raSolvedEnd.ToString("F5")}");
								AstroTools.RaDecToAzAlt(
											_raSolvedStart * 15.0,
											_decSolvedStart,
											AppSettings.Instance.SiteLatitude,
											AppSettings.Instance.SiteLongitude,
											DateTime.UtcNow,
											out double azimuthStart, out double altitudeStart);
								AstroTools.RaDecToAzAlt(
											_raSolvedEnd * 15.0,
											_decSolvedEnd,
											AppSettings.Instance.SiteLatitude,
											AppSettings.Instance.SiteLongitude,
											DateTime.UtcNow,
											out double azimuth, out double altitude);
								Log.WriteLine($"AXISCALIB: ALT/AZ calibration. ALT Start:{altitudeStart.ToString("F2")}. End:{altitude.ToString("F2")}");
								Log.WriteLine($"AXISCALIB: ALT/AZ calibration. AZ Start:{azimuthStart.ToString("F2")}. End:{azimuth.ToString("F2")}");
								// Assume ALT. ALT is unlikely to have a 0 crossing, but AS is very likely to cross 0/360 since we're supposed to be pointing North.
								float movedDegrees = (float)(altitude - altitudeStart);
								if (SelectedAxis == "AZ")
								{
									if (azimuth > 270)
									{
										azimuth -= 360;
									}
									if (azimuthStart > 270)
									{
										azimuthStart -= 360;
									}
									movedDegrees = (float)(azimuth - azimuthStart);
									Log.WriteLine($"AXISCALIB: ALT/AZ calibration. Corrected AZ Start:{azimuthStart.ToString("F2")}. End:{azimuth.ToString("F2")}. MovedDegrees now {movedDegrees.ToString("F3")}");
								}
								else
								{
									Log.WriteLine($"AXISCALIB: ALT/AZ calibration. ALT MovedDegrees {movedDegrees.ToString("F3")}");
								}
								CalculatedStepsPerDegree = Math.Abs(stepsMoved / movedDegrees);
								MountStepsPerDegree = SelectedAxis == "ALT" ? _mountVM.ALTStepsPerDegree : _mountVM.AZStepsPerDegree;
								Log.WriteLine($"AXISCALIB: ALT/AZ calibration. Calculated steps/deg {CalculatedStepsPerDegree.ToString("F1")}");
								Log.WriteLine($"AXISCALIB: ALT/AZ calibration. Mount steps/deg {MountStepsPerDegree.ToString("F1")}");
								if (MountStepsPerDegree == 0)
								{
									ErrorRatio = 0;
								}
								else
								{
									ErrorRatio = 100.0f * CalculatedStepsPerDegree / MountStepsPerDegree;
									Log.WriteLine($"AXISCALIB: ALT/AZ calibration. Error Ratio {ErrorRatio.ToString("F1")}%");
								}
								GenerateSuggestion(SelectedAxis, movedDegrees, stepsMoved);
							}
							break;
					}

					_calibrationState = CalibrationState.ConfirmResults;
					OnPropertyChanged("Step");

					break;

				case CalibrationState.ConfirmResults:
					WpfUtilities.RunOnUiThread(() => this.Close(), Application.Current.Dispatcher);
					break;
			}
		}
	}
}
