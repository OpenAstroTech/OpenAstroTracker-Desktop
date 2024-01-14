using MahApps.Metro.Controls;
using OATCommunications.Utilities;
using OATCommunications.WPF;
using OATControl.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
	/// Interaction logic for DlgStepCalibration.xaml
	/// </summary>
	public partial class DlgStepCalibration : MetroWindow, INotifyPropertyChanged
	{
		enum CalibrationState
		{
			WaitToStart,
			SlewToDecStart,
			GetDecStartCoordinate,
			Slew45DecDegrees,
			GetDecEndCoordinate,
			SlewBack15DecDegrees,
			GetRaStartCoordinates,
			Slew45RaDegrees,
			GetRAEndCoordinate,
			SlewBack45RaDegrees,
			ConfirmResults,
			SlewBackToHomeAndSet
		}
		private MountVM _mountVM;
		private float _raStepsBefore;
		private float _decStepsBefore;

		private float _raStepsAfter;
		private float _decStepsAfter;
		private float _degreesToOne = -15f;
		private float _degreesToTwo = 45f;
		private float _degreesToThree = -22.5f;
		private float _degreesToFour = 45f;

		public float _decSolvedStart;
		private string _inputCoordinate;
		public float _decSolvedEnd;
		public float _raSolvedStart;
		public float _raSolvedEnd;

		private long _currentStep;

		private CalibrationState _calibrationState;
		DelegateCommand _cancelCommand;
		DelegateCommand _continueCommand;
		DelegateCommand _closeCommand;
		private bool _displayStatus;
		private bool _canContinue = true;
		private DispatcherTimer _timer;

		public event PropertyChangedEventHandler PropertyChanged;

		public DlgStepCalibration(MountVM mountVM)
		{
			_mountVM = mountVM;
			_calibrationState = CalibrationState.WaitToStart;
			_raStepsBefore = _mountVM.RAStepsPerDegree;
			_decStepsBefore = _mountVM.DECStepsPerDegree;
			_raStepsAfter = _raStepsBefore;
			_decStepsAfter = _decStepsBefore;
			_displayStatus = false;

			_currentStep = 1;
			_cancelCommand = new DelegateCommand(() => OnCancel(), () => true);
			_continueCommand = new DelegateCommand(async () => await OnContinue(), () => true);
			_closeCommand = new DelegateCommand(() => OnClose(), () => true);

			_timer = new DispatcherTimer(TimeSpan.FromSeconds(1.0), DispatcherPriority.Normal, OnTimerTick, Application.Current.Dispatcher);

			this.DataContext = this;
			InitializeComponent();
		}


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
			// Issue STOP command to mount
			_mountVM.StopSlewingCommand.Execute(null);
			_mountVM.HomeCommand.Execute(null);
			this.Close();
		}

		public void OnClose()
		{
			this.Close();
		}

		public string InputCoordinate
		{
			get { return _inputCoordinate; }
			set
			{
				if (value != _inputCoordinate)
				{
					_inputCoordinate = value;
					OnPropertyChanged();
				}
			}
		}

		public float DegreesToOne
		{
			get { return _degreesToOne; }
			set
			{
				if (value != _degreesToOne)
				{
					_degreesToOne = value;
					OnPropertyChanged();
				}
			}
		}

		public float DegreesToTwo
		{
			get { return _degreesToTwo; }
			set
			{
				if (value != _degreesToTwo)
				{
					_degreesToTwo = value;
					OnPropertyChanged();
				}
			}
		}

		public float DegreesToThree
		{
			get { return _degreesToThree; }
			set
			{
				if (value != _degreesToThree)
				{
					_degreesToThree = value;
					OnPropertyChanged();
				}
			}
		}

		public float DegreesToFour
		{
			get { return _degreesToFour; }
			set
			{
				if (value != _degreesToFour)
				{
					_degreesToFour = value;
					OnPropertyChanged();
				}
			}
		}

		public float DecStepsBefore
		{
			get { return _decStepsBefore; }
			set
			{
				if (value != _decStepsBefore)
				{
					_decStepsBefore = value;
					OnPropertyChanged();
				}
			}
		}

		public float RaStepsBefore
		{
			get { return _raStepsBefore; }
			set
			{
				if (value != _raStepsBefore)
				{
					_raStepsBefore = value;
					OnPropertyChanged();
				}
			}
		}


		public float DecStepsAfter
		{
			get { return _decStepsAfter; }
			set
			{
				if (value != _decStepsAfter)
				{
					_decStepsAfter = value;
					OnPropertyChanged();
				}
			}
		}

		public float RaStepsAfter
		{
			get { return _raStepsAfter; }
			set
			{
				if (value != _raStepsAfter)
				{
					_raStepsAfter = value;
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

		public long Step
		{
			get { return _currentStep; }
			set
			{
				if (value != _currentStep)
				{
					_currentStep = value;
					OnPropertyChanged();
				}
			}
		}

		private void OnTimerTick(object sender, EventArgs e)
		{
			switch (_calibrationState)
			{
				case CalibrationState.SlewToDecStart:
				case CalibrationState.Slew45DecDegrees:
				case CalibrationState.SlewBack15DecDegrees:
				case CalibrationState.Slew45RaDegrees:
				case CalibrationState.SlewBack45RaDegrees:
				case CalibrationState.SlewBackToHomeAndSet:
					if (!_mountVM.IsSlewing('a'))
					{
						Log.WriteLine("STEPCALIBRATION: Slewing ended.");
						Task.Run(() => OnContinue());
						_timer.Stop();
					}
					break;
			}
		}

		public async Task OnContinue()
		{
			switch (_calibrationState)
			{
				case CalibrationState.WaitToStart:
					// Initiate slew 15 deg up, display status
					Log.WriteLine("STEPCALIBRATION: Moving mount up {0} deg.", _degreesToOne);
					await _mountVM.MoveMount(0, (long)(_decStepsBefore * _degreesToOne));
					DisplayStatus = true;
					await Task.Delay(500);
					_calibrationState = CalibrationState.SlewToDecStart;
					_timer.Start();
					break;

				case CalibrationState.SlewToDecStart:
					// if slewing complete
					{
						DisplayStatus = false;
						_calibrationState = CalibrationState.GetDecStartCoordinate;
						Step = 2;
						await _mountVM.UpdateStatus();
						Log.WriteLine("STEPCALIBRATION: Prompting 1st DEC value, default is {0} -> {1}.", _mountVM.CurrentDECTotalHours, MountVM.CoordToString(_mountVM.CurrentDECTotalHours));
						InputCoordinate = MountVM.CoordToString(_mountVM.CurrentDECTotalHours);
					}
					break;

				case CalibrationState.GetDecStartCoordinate:
					if (MountVM.TryParseCoord(InputCoordinate, out _decSolvedStart))
					{
						// Sync to platesolved position
						Log.WriteLine("STEPCALIBRATION: Platesolved DEC is, {0}, syncing mount to that.", _decSolvedStart, MountVM.CoordToString(_decSolvedStart));
						await _mountVM.SyncMountTo(_mountVM.CurrentRATotalHours, _decSolvedStart);

						Log.WriteLine("STEPCALIBRATION: Moving DEC up {0}deg.", _degreesToTwo);
						await _mountVM.MoveMount(0, (long)(_decStepsBefore * _degreesToTwo));
						DisplayStatus = true;
						await Task.Delay(500);

						_calibrationState = CalibrationState.Slew45DecDegrees;
						_timer.Start();
					}
					else
					{
						// Beep
					}
					break;

				case CalibrationState.Slew45DecDegrees:
					// Slewing complete
					{
						DisplayStatus = false;
						_calibrationState = CalibrationState.GetDecEndCoordinate;
						await _mountVM.UpdateStatus();
						Log.WriteLine("STEPCALIBRATION: Prompting 2nd DEC value, default is {0} -> {1}.", _mountVM.CurrentDECTotalHours, MountVM.CoordToString(_mountVM.CurrentDECTotalHours));
						InputCoordinate = MountVM.CoordToString(_mountVM.CurrentDECTotalHours);
						Step = 3;
					}
					break;

				case CalibrationState.GetDecEndCoordinate:
					if (MountVM.TryParseCoord(InputCoordinate, out _decSolvedEnd))
					{
						// Store end dec coords and stepper pos
						// Initiate 15 deg slew back using steps, display status
						Log.WriteLine("STEPCALIBRATION: Platesolved DEC end is, {0}. Moving east 1.5h.", _decSolvedStart, MountVM.CoordToString(_decSolvedEnd));
						await _mountVM.MoveMount((long)(_raStepsBefore * _degreesToThree), 0);
						DisplayStatus = true;
						await Task.Delay(500);
						_calibrationState = CalibrationState.SlewBack15DecDegrees;
						_timer.Start();
					}
					else
					{
						//beep
					}
					break;

				case CalibrationState.SlewBack15DecDegrees:
					// Slewing complete
					{
						DisplayStatus = false;
						_calibrationState = CalibrationState.GetRaStartCoordinates;
						await _mountVM.UpdateStatus();
						Log.WriteLine("STEPCALIBRATION: Starting RA calibration. Prompting for RA, default is {0} -> {1}.", _mountVM.CurrentRATotalHours, MountVM.CoordToString(_mountVM.CurrentRATotalHours));
						InputCoordinate = MountVM.CoordToString(_mountVM.CurrentRATotalHours);
						Step = 4;
					}
					break;

				case CalibrationState.GetRaStartCoordinates:
					if (MountVM.TryParseCoord(InputCoordinate, out _raSolvedStart))
					{
						// Sync to platesolved position
						Log.WriteLine("STEPCALIBRATION: Platesolved RA is, {0}, syncing mount to that.", _raSolvedStart, MountVM.CoordToString(_raSolvedStart));
						await _mountVM.SyncMountTo(_raSolvedStart, _mountVM.CurrentDECTotalHours);

						// Initiate 3h deg slew using steps, display status
						Log.WriteLine("STEPCALIBRATION: Moving RA 45deg.");
						await _mountVM.MoveMount((long)(_raStepsBefore * _degreesToFour), 0);
						DisplayStatus = true;
						await Task.Delay(500);


						_calibrationState = CalibrationState.Slew45RaDegrees;
						_timer.Start();

					}
					else
					{
						// Beep
					}
					break;

				case CalibrationState.Slew45RaDegrees:
					// Slewing complete
					{
						DisplayStatus = false;
						_calibrationState = CalibrationState.GetRAEndCoordinate;
						await _mountVM.UpdateStatus();
						Log.WriteLine("STEPCALIBRATION: Prompting 2nd RA value, default is {0} -> {1}.", _mountVM.CurrentRATotalHours, MountVM.CoordToString(_mountVM.CurrentRATotalHours));
						InputCoordinate = MountVM.CoordToString(_mountVM.CurrentRATotalHours);
						Step = 5;
					}
					break;

				case CalibrationState.GetRAEndCoordinate:
					if (MountVM.TryParseCoord(InputCoordinate, out _raSolvedEnd))
					{
						// Store end coordinates and pos
						// Initiate slew back in RA. display status
						Log.WriteLine("STEPCALIBRATION: Platesolved RA end is, {0}. Moving RA back to start by -45deg.", _raSolvedEnd, MountVM.CoordToString(_raSolvedEnd));
						await _mountVM.MoveMount((long)(-_raStepsBefore * (_degreesToThree + _degreesToFour)), (long)(-_decStepsBefore * (_degreesToOne + _degreesToTwo)));

						DisplayStatus = true;
						await Task.Delay(500);

						_calibrationState = CalibrationState.SlewBack45RaDegrees;
						_timer.Start();
					}
					else
					{
						// beep;
					}
					break;

				case CalibrationState.SlewBack45RaDegrees:
					// Slewing complete
					{
						DisplayStatus = false;
						_calibrationState = CalibrationState.ConfirmResults;
						Step = 6;

						// double raStepsTaken = 45 * _raStepsBefore;
						// double decStepsTaken = 45 * _decStepsBefore;
						// double secsPerDay = 86400;
						// double secsPerSiderealDay = 86164;
						// double secRatio = secsPerSiderealDay / secsPerDay;
						// double rotationPerDay = 360 * secRatio;
						// double rotationPerHour = rotationPerDay / 24.0;
						// double rotationPerMinute = rotationPerHour / 60.0;
						// double rotationPerSecond = rotationPerMinute / 60.0;

						// DEC
						double actualDecDegrees = _decSolvedStart - _decSolvedEnd;
						if (Math.Sign(_degreesToOne) != Math.Sign(_degreesToTwo))
						{
							actualDecDegrees = (90 - _decSolvedStart) + (90 - _decSolvedEnd);
						}
						double decRatio = _degreesToTwo / actualDecDegrees;
						DecStepsAfter = (float)(Math.Round(_decStepsBefore * decRatio * 10.0) / 10.0);

						// RA
						double actualRaHours = _raSolvedStart - _raSolvedEnd;
						if (actualRaHours < 0)
						{
							actualRaHours += 24.0;
						}
						if (actualRaHours > 12.0)
						{
							actualRaHours = 24.0 - actualRaHours;
						}

						double raRatio = Math.Abs(_degreesToFour / 15.0f / actualRaHours);
						RaStepsAfter = (float)(Math.Round(_raStepsBefore * raRatio * 10.0) / 10.0);

						Log.WriteLine("STEPCALIBRATION: RA Calculation");
						Log.WriteLine("STEPCALIBRATION:   Expected Move : {0:0.0000}h ({1:0.0000}deg)", _degreesToFour / 15.0, _degreesToFour);
						Log.WriteLine("STEPCALIBRATION:     Actual Move : {0:0.0000}h ({1:0.0000}deg)", actualRaHours, actualRaHours * 15);
						Log.WriteLine("STEPCALIBRATION:      Move Ratio : {0:0.000}", raRatio);
						Log.WriteLine("STEPCALIBRATION:       Old Steps : {0:0.0}", RaStepsBefore);
						Log.WriteLine("STEPCALIBRATION:       New Steps : {0:0.0}", RaStepsAfter);

						Log.WriteLine("STEPCALIBRATION: DEC Calculation");
						Log.WriteLine("STEPCALIBRATION:   Expected Move : {0:0.0000}deg", _degreesToTwo);
						Log.WriteLine("STEPCALIBRATION:     Actual Move : {0:0.0000}deg)", actualDecDegrees);
						Log.WriteLine("STEPCALIBRATION:      Move Ratio : {0:0.000}", decRatio);
						Log.WriteLine("STEPCALIBRATION:       Old Steps : {0:0.0}", DecStepsBefore);
						Log.WriteLine("STEPCALIBRATION:       New Steps : {0:0.0}", DecStepsAfter);
					}
					break;

				case CalibrationState.ConfirmResults:
					// Slew back to home
					DisplayStatus = true;
					_calibrationState = CalibrationState.SlewBackToHomeAndSet;
					_mountVM.HomeCommand.Execute(null);
					await Task.Delay(500);
					_timer.Start();
					break;

				case CalibrationState.SlewBackToHomeAndSet:
					// If slew complete
					{
						DisplayStatus = false;

						// set RA and DEC stepsperdegree according to formula
						await _mountVM.SetSteps(RaStepsAfter, DecStepsAfter);

						WpfUtilities.RunOnUiThread(() => this.Close(), Application.Current.Dispatcher);
					}
					break;
			}

			CanContinue = !DisplayStatus;
		}
	}
}
