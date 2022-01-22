using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using OATCommunications;
using OATCommunications.Model;
using OATCommunications.WPF.CommunicationHandlers;
using OATCommunications.Utilities;
using CommandResponse = OATCommunications.CommunicationHandlers.CommandResponse;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;
using System.Xml.Linq;

namespace OATControl.ViewModels
{
	public class MountVM : ViewModelBase
	{
		const long InitialIncrementalDelay = 250;
		DayTime _targetRA = new DayTime();
		Declination _targetDEC = new Declination();
		DayTime _currentRA = new DayTime();
		Declination _currentDEC = new Declination();

		long _raStepper = 0;
		long _decStepper = 0;
		string _decStepperTicks = "0";
		long _trkStepper = 0;
		long _focStepper = 0;
		float _raStepsPerDegree = 1;
		float _raStepsPerDegreeEdit = 1;
		float _raStepperMinimum = -7;
		float _raStepperMaximum = 7;
		double _raStepperTargetHours;

		float _decStepsPerDegree = 1;
		float _decStepsPerDegreeEdit = 1;
		float _decStepperMinimum = -90;
		float _decStepperMaximum = 180;
		float _decStepperLowerLimit = -90;
		float _decStepperUpperLimit = 180;
		double _decStepperTargetDegrees;
		bool _showDecLimits = false;

		bool _connected = false;
		bool _slewInProgress = false;
		bool _isTracking = false;
		bool _isGuiding = false;
		bool _isSlewingNorth = false;
		bool _isSlewingSouth = false;
		bool _isSlewingWest = false;
		bool _isSlewingEast = false;
		bool _driftAlignRunning = false;
		bool _keepMiniControllerOnTop = false;
		bool _inStartup = false;
		string _driftAlignStatus = "Drift Alignment";
		float _driftPhase = 0;
		DateTime _connectedAt = DateTime.UtcNow;
		string _lastDirection = string.Empty;

		private float _maxMotorSpeed = 2.5f;
		double _speed = 1.0;
		long _speedEdit = 0;
		string _scopeName = string.Empty;
		string _scopeVersion = string.Empty;
		string _scopeHardware = string.Empty;
		string _scopeRAStepper = string.Empty;
		string _scopeRADriver = "-";
		string _scopeDECStepper = string.Empty;
		string _scopeDECDriver = "-";
		string _scopeRASlewMS = "-";
		string _scopeRATrackMS = "-";
		string _scopeDECSlewMS = "-";
		string _scopeDECGuideMS = "-";
		bool _scopeHasAZ = false;
		bool _scopeHasALT = false;
		bool _scopeHasFOC = false;
		bool _scopeHasHSAH = false;
		string _scopeBoard = string.Empty;
		string _scopeDisplay = string.Empty;
		string _scopeFeatures = string.Empty;
		string _scopeLatitude = string.Empty;
		string _scopeLongitude = string.Empty;
		string _scopeTemperature = string.Empty;
		string _scopeDate = string.Empty;
		string _scopeTime = string.Empty;
		string _scopePolarisHourAngle = string.Empty;
		string _scopeSiderealTime = string.Empty;
		string _scopeNetworkState = string.Empty;
		string _scopeNetworkIPAddress = string.Empty;
		string _scopeNetworkSSID = string.Empty;
		string _connectionState = string.Empty;
		string _mountStatus = string.Empty;
		string _currentHA = string.Empty;
		CultureInfo _oatCulture = new CultureInfo("en-US");
		List<string> _oatAddonStates = new List<string>();
		string[] _customButtonText = new string[4];
		string[] _customCommandText = new string[4];
		string[] _customButtonResultText = new string[4];
		string[] _customButtonResultStatus = new string[4];

		DelegateCommand _arrowCommand;
		DelegateCommand _connectScopeCommand;
		DelegateCommand _slewToTargetCommand;
		DelegateCommand _syncToTargetCommand;
		DelegateCommand _syncToCurrentCommand;
		DelegateCommand _changeSlewingStateCommand;
		DelegateCommand _stopSlewingCommand;
		DelegateCommand _homeCommand;
		DelegateCommand _setHomeCommand;
		DelegateCommand _parkCommand;
		DelegateCommand _driftAlignCommand;
		DelegateCommand _polarAlignCommand;
		DelegateCommand _showLogFolderCommand;
		DelegateCommand _showSettingsCommand;
		DelegateCommand _showMiniControllerCommand;
		DelegateCommand _factoryResetCommand;
		DelegateCommand _startChangingCommand;
		DelegateCommand _chooseTargetCommand;
		DelegateCommand _setDecLowerLimitCommand;
		DelegateCommand _setDECHomeOffsetFromPowerOn;
		DelegateCommand _setRAHomeOffsetCommand;
		DelegateCommand _gotoDECHomeFromPowerOn;
		DelegateCommand _autoHomeRACommand;
		DelegateCommand _gotoDECParkBeforePowerOff;
		DelegateCommand _focuserResetCommand;
		DelegateCommand _customActionCommand;

		MiniController _miniController;
		TargetChooser _targetChooser;

		DispatcherTimer _timerStatus;
		DispatcherTimer _timerFineSlew;

		private ICommunicationHandler _commHandler;
		private string _serialBaudRate;

		private SimulationServer _oatSimComm = new SimulationServer();

		private OatmealTelescopeCommandHandlers _oatMount;
		private PointsOfInterest _pointsOfInterest;
		PointOfInterest _selectedPointOfInterest;
		private long _firmwareVersion = 0;
		private float _slewYSpeed;
		private float _slewXSpeed;
		private int _slewRate = 4;
		private object _speedUpdateLock = new object();
		private bool _updatedSpeeds;
		private bool _isCoarseSlewing = true;
		DateTime _startTime;
		private string _parkString = "Park";
		private string _remainingRATime;
		private long _slewStartRA;
		private long _slewStartDEC;
		private long _slewTargetRA;
		private long _slewTargetDEC;
		private bool _slewTargetValid;
		private float _trackingSpeed;
		private DispatcherTimer _timer;
		private float _incrementalDelay = InitialIncrementalDelay;
		private int _incrDirection;
		private string _incrVar;
		private string _keyboardFocus = string.Empty;

		public float RASpeed
		{
			get; private set;
		}
		public float DECSpeed
		{
			get; private set;
		}
		public MountVM()
		{
			Log.WriteLine("Mount: Initialization starting...");

			Log.WriteLine("Mount: Initializing communication handler factory...");
			CommunicationHandlerFactory.Initialize();

			Log.WriteLine("Mount: Checking whether ASCOM driver is present...");
			string location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			string ascomDriver = Path.Combine(location, "OATCommunications.ASCOM.dll");
			if (File.Exists(ascomDriver))
			{
				try
				{
					Log.WriteLine("Mount: Loading ASCOM driver ...");
					var ascom = Assembly.LoadFrom(ascomDriver);
					//ascom.GetExportedTypes().FirstOrDefault((t) => t.;
					var types = ascom.GetTypes();
					for (int i = 0; i < types.Length; i++)
					{
						Type type = ascom.GetType(types[i].FullName);
						if (type.GetInterface("ICommunicationHandler") != null)
						{
							object obj = Activator.CreateInstance(type);
							if (obj != null)
							{
								CommunicationHandlerFactory.AddHandler(obj as ICommunicationHandler);
								Log.WriteLine("Mount: ASCOM driver successfully added...");
							}
						}
					}
				}
				catch (Exception ex)
				{
					Log.WriteLine("Mount: Failed to load ASCOM driver. " + ex.Message);
				}
			}
			else
			{
				Log.WriteLine("Mount: ASCOM driver is not present. Expected at " + ascomDriver);
			}

			Log.WriteLine("Mount: Initiating communication device discovery...");
			CommunicationHandlerFactory.DiscoverDevices();
			Log.WriteLine("Mount: Device discovery started...");

			_startTime = DateTime.UtcNow;
			_timerStatus = new DispatcherTimer(TimeSpan.FromMilliseconds(500), DispatcherPriority.Normal, async (s, e) => await OnTimer(s, e), Application.Current.Dispatcher);
			_timerFineSlew = new DispatcherTimer(TimeSpan.FromMilliseconds(200), DispatcherPriority.Normal, async (s, e) => await OnFineSlewTimer(s, e), Application.Current.Dispatcher);
			_arrowCommand = new DelegateCommand(s => OnAdjustTarget(s.ToString()));
			_connectScopeCommand = new DelegateCommand(() => OnConnectToTelescope());
			_slewToTargetCommand = new DelegateCommand(async () => await OnSlewToTarget(), () => MountConnected);
			_syncToTargetCommand = new DelegateCommand(async () => await OnSyncToTarget(), () => MountConnected);
			_syncToCurrentCommand = new DelegateCommand(() => OnSyncToCurrent(), () => MountConnected);
			_changeSlewingStateCommand = new DelegateCommand(async s => await OnChangeSlewingState(s.ToString()), () => MountConnected);
			_stopSlewingCommand = new DelegateCommand(async () => await OnStopSlewing('*'), () => MountConnected);
			_homeCommand = new DelegateCommand(async () => await OnHome(), () => MountConnected);
			_setHomeCommand = new DelegateCommand(async () => await OnSetHome(), () => MountConnected);
			_parkCommand = new DelegateCommand(async () => await OnPark(), () => MountConnected);
			_driftAlignCommand = new DelegateCommand(async dur => await OnRunDriftAlignment(int.Parse(dur.ToString())), () => MountConnected);
			_polarAlignCommand = new DelegateCommand(() => OnRunPolarAlignment(), () => MountConnected);
			_showLogFolderCommand = new DelegateCommand(() => OnShowLogFolder(), () => true);
			_showSettingsCommand = new DelegateCommand(() => OnShowSettingsDialog(), () => true);
			_showMiniControllerCommand = new DelegateCommand(() => OnShowMiniController(), () => true);
			_factoryResetCommand = new DelegateCommand(() => OnPerformFactoryReset(), () => MountConnected);
			_startChangingCommand = new DelegateCommand((p) => OnStartChangingParameter(p), () => MountConnected);
			_chooseTargetCommand = new DelegateCommand(async (p) => await OnShowTargetChooser(), () => MountConnected);
			_setDecLowerLimitCommand = new DelegateCommand((p) => SetDecLowLimit(), () => MountConnected);
			_setDECHomeOffsetFromPowerOn = new DelegateCommand((p) => OnSetDECHomeOffsetFromPowerOn(), () => MountConnected);
			_setRAHomeOffsetCommand = new DelegateCommand((p) => OnSetRAHomeOffset(), () => MountConnected && (FirmwareVersion >= 10921) && ScopeHasHSAH);
			_gotoDECHomeFromPowerOn = new DelegateCommand((p) => OnGotoDECHomeFromPowerOn(), () => MountConnected && (FirmwareVersion > 10915));
			_autoHomeRACommand = new DelegateCommand((p) => OnAutoHomeRA(), () => MountConnected && (FirmwareVersion >= 10921) && ScopeHasHSAH);
			_gotoDECParkBeforePowerOff = new DelegateCommand((p) => OnGotoDECParkBeforePowerOff(), () => MountConnected && (FirmwareVersion > 10915));
			_focuserResetCommand = new DelegateCommand((p) => OnResetFocuserPosition(), () => MountConnected && (FirmwareVersion > 10918));
			_customActionCommand = new DelegateCommand((p) => OnCustomAction(p as string), () => MountConnected);

			var poiFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "PointsOfInterest.xml");
			Log.WriteLine("Mount: Attempting to read Point of Interest from {0}...", poiFile);
			if (File.Exists(poiFile))
			{
				_pointsOfInterest = new PointsOfInterest();
				_pointsOfInterest.ReadFromXml(poiFile);
				_selectedPointOfInterest = null;
				_pointsOfInterest.CalcDistancesFrom(CurrentRATotalHours, CurrentDECTotalHours, 0, 0);
				Log.WriteLine("Mount: Successfully read {0} Points of Interest.", _pointsOfInterest.Count - 1);
			}

			this.Version = Assembly.GetExecutingAssembly().GetName().Version;
			Log.WriteLine("Mount: Initialization of OATControl {0} complete...", this.Version);

			AppSettings.Instance.UpgradeVersion += OnUpgradeSettings;
			AppSettings.Instance.Load();
			LoadCustomCommands();

			// Start Simulation Server
			_oatSimComm.Start(4035);
			_oatSimComm.ClientConnected += OnSimulationClientConnect;
			_oatSimComm.ClientCommand += OnSimationClientCommand;
		}

		private void LoadCustomCommands()
		{
			XDocument doc = XDocument.Parse(AppSettings.Instance.CustomCommands);
			int i = 0;
			foreach (var elem in doc.Element("CustomCommands").Elements("Command"))
			{
				_customButtonText[i] = elem.Attribute("Display").Value;
				_customCommandText[i] = elem.Attribute("LX200Command").Value;
				if (string.IsNullOrEmpty(_customCommandText[i]))
				{
					_customButtonText[i] = "action " + (i + 1);
					_customButtonResultStatus[i] = "Ctrl-Shift-Click to set.";
				}
				if (i < 3) i++;
			}
			for (; i < 4; i++)
			{
				_customButtonText[i] = "action " + (i + 1);
				_customButtonResultStatus[i] = "Ctrl-Shift-Click to set.";
			}
		}

		private void SaveCustomCommands()
		{
			XDocument doc = new XDocument();
			doc.Add(
				new XElement("CustomCommands",
					new XElement("Command", new XAttribute("Display", _customButtonText[0]), new XAttribute("LX200Command", _customCommandText[0] ?? "")),
					new XElement("Command", new XAttribute("Display", _customButtonText[1]), new XAttribute("LX200Command", _customCommandText[1] ?? "")),
					new XElement("Command", new XAttribute("Display", _customButtonText[2]), new XAttribute("LX200Command", _customCommandText[2] ?? "")),
					new XElement("Command", new XAttribute("Display", _customButtonText[3]), new XAttribute("LX200Command", _customCommandText[3] ?? ""))
					)
			);

			AppSettings.Instance.CustomCommands = doc.ToString();
			AppSettings.Instance.Save();
		}

		public void OnUpgradeSettings(object sender, UpgradeEventArgs e)
		{
			// If needed upgrade the settings file between versions here.
			// e.LoadedVersion vs. e.CurrentVersion;
		}

		public void OnResetFocuserPosition()
		{
			// 50000 seems to be the standard focuser rest position
			_oatMount.SendCommand($":FP50000#,n", (a) => { });
		}

		public void OnCustomAction(string nr)
		{
			int cmdNr = int.Parse(nr);
			if (Keyboard.IsKeyDown(Key.LeftShift) && Keyboard.IsKeyDown(Key.LeftCtrl))
			{
				var dlg = new DlgCustomActionSetup() { WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault() };
				dlg.ButtonText = _customButtonText[cmdNr];
				dlg.CommandText = _customCommandText[cmdNr];

				dlg.ShowDialog();
				if (dlg.DialogResult == true)
				{
					_customButtonText[cmdNr] = dlg.ButtonText;
					_customCommandText[cmdNr] = dlg.CommandText;
					OnPropertyChanged($"CustomAction{cmdNr + 1}Text");
					SaveCustomCommands();
				}
			}
			else if (!string.IsNullOrEmpty(_customCommandText[cmdNr]))
			{
				_customButtonResultStatus[cmdNr] = "Sending...";
				_customButtonResultText[cmdNr] = string.Empty;
				OnPropertyChanged($"CustomAction{cmdNr + 1}ResultText");
				OnPropertyChanged($"CustomAction{cmdNr + 1}ResultStatus");
				string command = _customCommandText[cmdNr];
				_oatMount.SendCommand(command, (response) =>
				{
					if (!response.Success)
					{
						_customButtonResultStatus[cmdNr] = "Failed. ";
						_customButtonResultText[cmdNr] = response.StatusMessage;
					}
					else
					{
						_customButtonResultStatus[cmdNr] = "Succeeded.";
						_customButtonResultText[cmdNr] = response.Data;
					}

					OnPropertyChanged($"CustomAction{cmdNr + 1}ResultStatus");
					OnPropertyChanged($"CustomAction{cmdNr + 1}ResultText");
				});
			}
		}

		public string CustomAction1Text
		{
			get
			{
				return _customButtonText[0];
			}
			set
			{
				if (_customButtonText[0] != value)
				{
					_customButtonText[0] = value;
					OnPropertyChanged();
				}
			}
		}

		public string CustomAction2Text
		{
			get
			{
				return _customButtonText[1];
			}
			set
			{
				if (_customButtonText[1] != value)
				{
					_customButtonText[1] = value;
					OnPropertyChanged();
				}
			}
		}

		public string CustomAction3Text
		{
			get
			{
				return _customButtonText[2];
			}
			set
			{
				if (_customButtonText[2] != value)
				{
					_customButtonText[2] = value;
					OnPropertyChanged();
				}
			}
		}

		public string CustomAction4Text
		{
			get
			{
				return _customButtonText[3];
			}
			set
			{
				if (_customButtonText[3] != value)
				{
					_customButtonText[3] = value;
					OnPropertyChanged();
				}
			}
		}

		public string CustomAction1ResultText
		{
			get
			{
				return _customButtonResultText[0];
			}
			set
			{
				if (_customButtonResultText[0] != value)
				{
					_customButtonResultText[0] = value;
					OnPropertyChanged();
				}
			}
		}

		public string CustomAction2ResultText
		{
			get
			{
				return _customButtonResultText[1];
			}
			set
			{
				if (_customButtonResultText[1] != value)
				{
					_customButtonResultText[1] = value;
					OnPropertyChanged();
				}
			}
		}

		public string CustomAction3ResultText
		{
			get
			{
				return _customButtonResultText[2];
			}
			set
			{
				if (_customButtonResultText[2] != value)
				{
					_customButtonResultText[2] = value;
					OnPropertyChanged();
				}
			}
		}

		public string CustomAction4ResultText
		{
			get
			{
				return _customButtonResultText[3];
			}
			set
			{
				if (_customButtonResultText[3] != value)
				{
					_customButtonResultText[3] = value;
					OnPropertyChanged();
				}
			}
		}
		public string CustomAction1ResultStatus
		{
			get
			{
				return _customButtonResultStatus[0];
			}
			set
			{
				if (_customButtonResultStatus[0] != value)
				{
					_customButtonResultStatus[0] = value;
					OnPropertyChanged();
				}
			}
		}

		public string CustomAction2ResultStatus
		{
			get
			{
				return _customButtonResultStatus[1];
			}
			set
			{
				if (_customButtonResultStatus[1] != value)
				{
					_customButtonResultStatus[1] = value;
					OnPropertyChanged();
				}
			}
		}

		public string CustomAction3ResultStatus
		{
			get
			{
				return _customButtonResultStatus[2];
			}
			set
			{
				if (_customButtonResultStatus[2] != value)
				{
					_customButtonResultStatus[2] = value;
					OnPropertyChanged();
				}
			}
		}

		public string CustomAction4ResultStatus
		{
			get
			{
				return _customButtonResultStatus[3];
			}
			set
			{
				if (_customButtonResultStatus[3] != value)
				{
					_customButtonResultStatus[3] = value;
					OnPropertyChanged();
				}
			}
		}

		public void OnAutoHomeRA()
		{
			_oatMount.SendCommand($":MHRR#,n", (a) => { });
		}

		public void OnGotoDECHomeFromPowerOn()
		{
			if (AppSettings.Instance.DECHomeOffset != 0)
			{
				_oatMount.SendCommand($":MXd{AppSettings.Instance.DECHomeOffset}#,n", (a) => { });
			}
			else
			{
				var dlg = new DlgMessageBox("No DEC axis offset is stored. Slew to Home position from Off position and click 'Set DEC Home Offset'.");
				dlg.ShowDialog();
			}
		}

		public void OnGotoDECParkBeforePowerOff()
		{
			if (AppSettings.Instance.DECHomeOffset != 0)
			{
				if (DECStepper != 0)
				{
					var dlg = new DlgMessageBox("DEC axis is not in Home position. Please slew Home before parking.");
					dlg.ShowDialog();
				}
				else
				{
					_oatMount.SendCommand($":MXd{-AppSettings.Instance.DECHomeOffset}#,n", (a) => { });
				}
			}
		}

		public void OnSetRAHomeOffset()
		{
			var donePosQuery = new AutoResetEvent(false);
			long raPos = -1, decPos = -1;
			bool posValid = false;
			_oatMount.SendCommand(":GX#,#", (a) =>
			{
				if (a.Success)
				{
					posValid = true;
					var parts = a.Data.Split(',');
					posValid = posValid && long.TryParse(parts[2], out raPos);
					posValid = posValid && long.TryParse(parts[3], out decPos);
					donePosQuery.Set();
				}
			});
			donePosQuery.WaitOne();
			if (posValid)
			{
				_oatMount.SendCommand($":XSHR{-raPos}#", (a) => { });
				AppSettings.Instance.RAHomeOffset = raPos;
				AppSettings.Instance.Save();
			}
		}


		public void OnSetDECHomeOffsetFromPowerOn()
		{
			var donePosQuery = new AutoResetEvent(false);
			long raPos = -1, decPos = -1;
			bool posValid = false;
			_oatMount.SendCommand(":GX#,#", (a) =>
			{
				if (a.Success)
				{
					posValid = true;
					var parts = a.Data.Split(',');
					posValid = posValid && long.TryParse(parts[2], out raPos);
					posValid = posValid && long.TryParse(parts[3], out decPos);
					donePosQuery.Set();
				}
			});
			donePosQuery.WaitOne();
			if (posValid)
			{
				if (decPos == 0)
				{
					var dlg = new DlgMessageBox("DEC axis is at zero position. Skipping.\nMove Axis manually to Home position and click again.");
					dlg.ShowDialog();
				}
				else
				{
					AppSettings.Instance.DECHomeOffset = decPos;
					AppSettings.Instance.Save();
				}
			}
		}

		private void OnStartChangingParameter(object p)
		{
			if (_timer == null)
			{
				_timer = new DispatcherTimer(TimeSpan.FromMilliseconds(InitialIncrementalDelay), DispatcherPriority.Render, OnIncrementVariable, Dispatcher.CurrentDispatcher);
			}

			string par = p as string;
			if (par[0] == '+')
			{
				_timer.Interval = TimeSpan.FromMilliseconds(_incrementalDelay);
				_timer.Start();
			}
			else if (par[0] == '-')
			{
				_timer.Stop();
				_incrementalDelay = InitialIncrementalDelay;
				if (par[1] == 'S')
				{
					RAStepsPerDegree = RAStepsPerDegreeEdit;
					SetDECStepsPerDegree(DECStepsPerDegreeEdit);
					SpeedCalibrationFactor = 1.0 + SpeedCalibrationFactorEdit / 10000.0f;
				}
			}

			_incrDirection = par[3] == '+' ? 1 : -1;
			_incrVar = $"{par[1]}{par[2]}";
			if (par[0] == '+')
			{
				switch (_incrVar)
				{
					case "SR": RAStepsPerDegreeEdit += 0.1f * _incrDirection; break;
					case "SD": DECStepsPerDegreeEdit += 0.1f * _incrDirection; break;
					case "SS": SpeedCalibrationFactorEdit += _incrDirection; break;
					default: OnAdjustTarget(_incrVar + (_incrDirection == 1 ? "+" : "-")); break;
				}
			}
		}

		void SetDECStepsPerDegree(float steps)
		{
			DECStepsPerDegree = steps;
		}

		private void OnIncrementVariable(object sender, EventArgs e)
		{
			_timer.Stop();
			_incrementalDelay = (float)Math.Max(2, 0.9 * _incrementalDelay);
			switch (_incrVar)
			{
				case "SR": RAStepsPerDegreeEdit += 0.2f * _incrDirection; break;
				case "SD": DECStepsPerDegreeEdit += 0.2f * _incrDirection; break;
				case "SS": SpeedCalibrationFactorEdit += _incrDirection; break;
				default: OnAdjustTarget(_incrVar + (_incrDirection == 1 ? "+" : "-")); break;
			}
			_timer.Interval = TimeSpan.FromMilliseconds(_incrementalDelay);
			_timer.Start();
		}

		private async Task OnPerformFactoryReset()
		{
			if (MessageBox.Show("Are you sure you want to erase all stored settings on the OAT?", "Confirm Factory Reset", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes)
			{
				var doneEvent = new ManualResetEvent(false);
				this.SendOatCommand(":XFR#,n", (a) => { doneEvent.Set(); });
				doneEvent.WaitOne();
				await UpdateInitialScopeValues();
				RAStepsPerDegreeEdit = RAStepsPerDegree;
				DECStepsPerDegreeEdit = DECStepsPerDegree;
				SpeedCalibrationFactorEdit = (long)Math.Round((SpeedCalibrationFactor - 1.0) * 10000);
				var message = "OAT has been reset.";
				if (_firmwareVersion < 10872)
				{
					message += " Please disconnect, reset OAT and reconnect.";
				}
				MessageBox.Show(message, "Factory Reset complete", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}

		private async Task OnShowTargetChooser()
		{
			if (_targetChooser == null)
			{
				_targetChooser = new TargetChooser(this);
				if (AppSettings.Instance.TargetChooserPos.X != -1)
				{
					_targetChooser.Left = AppSettings.Instance.TargetChooserPos.X;
					_targetChooser.Top = AppSettings.Instance.TargetChooserPos.Y;
					_targetChooser.Width = AppSettings.Instance.TargetChooserSize.Width;
					_targetChooser.Height = AppSettings.Instance.TargetChooserSize.Height;
				}
			}

			if (_targetChooser.IsVisible)
			{
				_targetChooser.Hide();
			}
			else
			{
				_targetChooser.Show();
				await RecalculatePointsPositions(true);
			}
		}

		private void OnShowMiniController()
		{
			if (_miniController == null)
			{
				_miniController = new MiniController(this);
				_miniController.Topmost = KeepMiniControlOnTop;

				if (AppSettings.Instance.MiniControllerPos.X != -1)
				{
					_miniController.Left = AppSettings.Instance.MiniControllerPos.X;
					_miniController.Top = AppSettings.Instance.MiniControllerPos.Y;
				}
			}
			if (_miniController.IsVisible)
			{
				_miniController.Hide();
			}
			else
			{
				_miniController.Show();
			}
		}

		private void OnShowSettingsDialog()
		{
			SettingsDialog dlg = new SettingsDialog(this) { WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault() };
			dlg.ShowDialog();
		}

		private void OnShowLogFolder()
		{
			ProcessStartInfo info = new ProcessStartInfo("explorer.exe", Path.GetDirectoryName(Log.Filename)) { UseShellExecute = true };
			Process.Start(info);
		}

		public bool IsLoggingEnabled
		{
			get
			{
				return Log.IsLoggingEnabled;
			}
		}

		private async Task OnSetHome()
		{
			if (Keyboard.IsKeyDown(Key.LeftShift))
			{
				AppSettings.Instance.DECHomeOffset = DECStepper;
				AppSettings.Instance.Save();
			}

			Log.WriteLine("Mount: Setting Home...");
			this.SendOatCommand(":SHP#,n", (a) => { });
			await ReadHA();
			await RecalculatePointsPositions(true);
		}

		private async Task RecalculatePointsPositions(bool force = false)
		{
			// For newer firmware, calculate the stepper positions for each point of interest.
			if (_firmwareVersion > 10900)
			{
				if ((_targetChooser != null) && _targetChooser.IsVisible)
				{
					var waiter = new AsyncAutoResetEvent();
					Log.WriteLine("POINT: Scheduling target updates");
					foreach (var point in _pointsOfInterest)
					{
						if (!point.IsPositionCalculated || force)
						{
							this.SendOatCommand(string.Format(_oatCulture, ":XGC{0:0.000}*{1:0.000}#,#", point.RA, point.DEC), (res) =>
							{
								if (res.Success)
								{
									var parts = res.Data.Split('|');
									point.SetPositions(long.Parse(parts[0]), long.Parse(parts[1]));
									point.CalcDistancesFrom(CurrentRATotalHours, CurrentDECTotalHours, RAStepper, DECStepper);
									Log.WriteLine("POINT: {0} is at {1}, {2}. From {3:0.00}, {4:0.00} it is {5:0.000} away.", point.Name, point.RAPosition, point.DECPosition, CurrentRATotalHours, CurrentDECTotalHours, point.Distance);
								}
							});
						}
					}

					this.SendOatCommand(":GVP#,#", (res) => { Log.WriteLine("POINT: Setting waiter"); waiter.Set(); });
					await waiter.WaitAsync();
					OnDECStepperLimitsChanged(0, 0);
					OnRaOrDecChanged(0, 0);
				}
			}
		}

		private async Task ReadHA()
		{
			Log.WriteLine("Mount: Reading HA");
			var waiter = new AsyncAutoResetEvent();
			this.SendOatCommand(":XGH#,#", (ha) =>
			{
				if (ha.Success)
				{

					CurrentHA = $"{ha.Data.Substring(0, 2)}h {ha.Data.Substring(2, 2)}m {ha.Data.Substring(4, 2)}s";
				}
				waiter.Set();
			});
			await waiter.WaitAsync();
		}

		private async Task OnTimer(object s, EventArgs e)
		{
			_timerStatus.Stop();
			if (MountConnected)
			{
				if (!_oatMount.Connected)
				{
					if (_commHandler.Connect())
					{
						MountConnected = true;
					}
					else
					{
						Disconnect();
						ScopeName = "Connection Lost...";
						ConnectionState = string.Empty;
					}
				}
				if (_oatMount?.Connected ?? false)
				{
					await UpdateStatus();
				}
			}

			_timerStatus.Start();
		}

		private async Task UpdateStatus()
		{
			var doneEvent = new AsyncAutoResetEvent();

			OnPropertyChanged("ConnectedTime");

			if (MountConnected)
			{
				await UpdateSiderealTime();
				// We can have a disconnect occur during the await of the call above.
				if (_oatMount != null)
				{
					this.SendOatCommand(":GX#,#", async (result) =>
					{
						if (result.Success)
						{
							//   0   1  2 3 4    5      6     7
							// Idle,--T,0,0,31,080300,+900000,50000,#
							string status = result.Data;
							if (result.Success && !string.IsNullOrWhiteSpace(status))
							{
								try
								{
									var parts = status.Split(",".ToCharArray());
									string prevStatus = MountStatus;
									_isGuiding = false;
									MountStatus = parts[0];
									if ((MountStatus == "SlewToTarget") && (prevStatus != "SlewToTarget"))
									{
										AsyncAutoResetEvent waitForTarget = new AsyncAutoResetEvent();
										this.SendOatCommand(":Gr#,#", (raRes) =>
										{
											if (raRes.Success)
											{
												_targetRA.SetTime(int.Parse(raRes.Data.Substring(0, 2)), int.Parse(raRes.Data.Substring(3, 2)), int.Parse(raRes.Data.Substring(6, 2)));
												_slewTargetRA = _targetRA.TotalSeconds;
											}
										});

										this.SendOatCommand(":Gd#,#", (decRes) =>
										{
											if (decRes.Success)
											{
												_targetDEC.SetTime(int.Parse(decRes.Data.Substring(0, 3)), int.Parse(decRes.Data.Substring(4, 2)), int.Parse(decRes.Data.Substring(7, 2)));
												_slewTargetDEC = _targetDEC.TotalSeconds;
											}
											waitForTarget.Set();
										});

										await waitForTarget.WaitAsync();

										// For newer firmware, use the actual stepper position calculations for progress display.
										_slewTargetValid = false;
										if (_firmwareVersion > 10900)
										{
											this.SendOatCommand(string.Format(_oatCulture, ":XGC{0:0.000}*{1:0.000}#,#", TargetRATotalHours, TargetDECTotalHours), (posRes) =>
											{
												if (posRes.Success)
												{
													var posParts = posRes.Data.Split('|');
													_slewTargetRA = long.Parse(posParts[0], _oatCulture);
													_slewTargetDEC = long.Parse(posParts[1], _oatCulture);
													Log.WriteLine("Mount: Slew Target stepper pos is {0}, {1}", _slewTargetRA, _slewTargetDEC);
													_slewTargetValid = true;
												}
												waitForTarget.Set();
											});
											await waitForTarget.WaitAsync();

											_slewStartRA = RAStepper;
											_slewStartDEC = DECStepper;
											Log.WriteLine("Mount: Slewing stepper from {0}, {1} to {2}, {3}", _slewStartRA, _slewStartDEC, _slewTargetRA, _slewTargetDEC);
										}
										else
										{
											_slewStartRA = _currentRA.TotalSeconds;
											_slewStartDEC = _currentDEC.TotalSeconds;
										}
										OnPropertyChanged("RASlewProgress");
										OnPropertyChanged("DECSlewProgress");

										DisplaySlewProgress = _slewTargetValid;
									}
									else if ((MountStatus != "SlewToTarget") && (prevStatus == "SlewToTarget"))
									{
										DisplaySlewProgress = false;
										await RecalculatePointsPositions(false);
									}
									else if (MountStatus == "SlewToTarget")
									{
										OnPropertyChanged("RASlewProgress");
										OnPropertyChanged("DECSlewProgress");
									}
									else if (MountStatus == "Guiding")
									{
										_isGuiding = true;
										_isTracking = true;
									}

									switch (parts[1][0])
									{
										case 'R': IsSlewingEast = true; IsSlewingWest = false; break;
										case 'r': IsSlewingEast = false; IsSlewingWest = true; break;
										default: IsSlewingEast = false; IsSlewingWest = false; break;
									}
									switch (parts[1][1])
									{
										case 'd': IsSlewingNorth = true; IsSlewingSouth = false; break;
										case 'D': IsSlewingNorth = false; IsSlewingSouth = true; break;
										default: IsSlewingNorth = false; IsSlewingSouth = false; break;
									}

									// Don't use property here since it sends a command.
									_isTracking = (parts[1][2] == 'T') || _isGuiding;
									OnPropertyChanged("IsTracking");
									OnPropertyChanged("IsGuiding");

									RAStepper = int.Parse(parts[2]);
									DECStepper = int.Parse(parts[3]);
									TrkStepper = int.Parse(parts[4]);

									_currentRA.SetTime(int.Parse(parts[5].Substring(0, 2)), int.Parse(parts[5].Substring(2, 2)), int.Parse(parts[5].Substring(4, 2)));
									_currentDEC.SetTime(int.Parse(parts[6].Substring(1, 2)), int.Parse(parts[6].Substring(3, 2)), int.Parse(parts[6].Substring(5, 2)));
									if (parts[6][0] == '-')
									{
										_currentDEC = Declination.FromSeconds(-_currentDEC.TotalSeconds);
									}
									UpdateSimulationClient();

									CurrentRAHour = int.Parse(parts[5].Substring(0, 2));
									CurrentRAMinute = int.Parse(parts[5].Substring(2, 2));
									CurrentRASecond = int.Parse(parts[5].Substring(4, 2));

									OnTargetChanged(0, 0);
									UpdateCurrentDisplay();

									if (parts.Length > 8)
									{
										int focusStepper;
										if (int.TryParse(parts[7], out focusStepper))
										{
											FocStepper = focusStepper;
										}
									}



								}
								catch (Exception ex)
								{
									Log.WriteLine("UpdateStatus: Failed to process GX reply [{0}]", status);
								}
							}
							else
							{
								Log.WriteLine("UpdateStatus: OAT command GX returned empty string");
							}
						}
						doneEvent.Set();
					});
				}
				else
				{
					doneEvent.Set();
				}

				await doneEvent.WaitAsync();
			}
		}

		public void UpdateTargetDisplay(bool force)
		{
			if (_keyboardFocus.Length == 0 || _keyboardFocus[0] != 'R' || force)
			{
				OnPropertyChanged("TargetRAHour");
				OnPropertyChanged("TargetRAMinute");
				OnPropertyChanged("TargetRASecond");
				OnPropertyChanged("TargetRATotalHours");
			}
			if (_keyboardFocus.Length == 0 || _keyboardFocus[0] != 'D' || force)
			{
				OnPropertyChanged("TargetDECSign");
				OnPropertyChanged("TargetDECDegree");
				OnPropertyChanged("TargetDECMinute");
				OnPropertyChanged("TargetDECSecond");
				OnPropertyChanged("TargetDECTotalHours");
			}
		}

		public void UpdateCurrentDisplay()
		{
			OnPropertyChanged("CurrentRAHour");
			OnPropertyChanged("CurrentRAMinute");
			OnPropertyChanged("CurrentRASecond");
			OnPropertyChanged("CurrentRATotalHours");
			OnPropertyChanged("CurrentDECSign");
			OnPropertyChanged("CurrentDECDegree");
			OnPropertyChanged("CurrentDECMinute");
			OnPropertyChanged("CurrentDECSecond");
			OnPropertyChanged("CurrentDECTotalHours");
		}

		public async Task<string> SetSiteLatitude(float latitude)
		{
			string result = await _oatMount.SetSiteLatitude(latitude);
			AppSettings.Instance.SiteLatitude = latitude;
			UpdateLocation();
			return result;
		}

		public async Task<string> SetSiteLongitude(float longitude)
		{
			string result = await _oatMount.SetSiteLongitude(longitude);
			AppSettings.Instance.SiteLongitude = longitude;
			UpdateLocation();
			return result;
		}

		public async Task UpdateSiderealTime()
		{
			string siderealTime = string.Empty;
			var done = new AsyncAutoResetEvent();
			this.SendOatCommand(":XGL#,#", (a) => { siderealTime = a.Data; done.Set(); });
			await done.WaitAsync();
			if (siderealTime.Length == 6)
			{
				ScopeSiderealTime = $"{siderealTime.Substring(0, 2)}:{siderealTime.Substring(2, 2)}:{siderealTime.Substring(4)}";
			}
			else
			{
				ScopeSiderealTime = "-";
			}
		}

		public void UpdateRealtimeParameters(bool editable)
		{
			string ha = string.Empty;
			string utcOffset = string.Empty;
			string localTime = string.Empty;
			string localDate = string.Empty;
			string siderealTime = string.Empty;
			string temperature = string.Empty;
			string speed = string.Empty;
			string network = string.Empty;
			bool failed = false;

			if (_oatMount != null)
			{
				ManualResetEvent allDone = new ManualResetEvent(false);
				this.SendOatCommand(":GG#,#", (a) => { utcOffset = a.Data; failed |= !a.Success; });
				this.SendOatCommand(":GL#,#", (a) => { localTime = a.Data; failed |= !a.Success; });
				this.SendOatCommand(":GC#,#", (a) => { localDate = a.Data; failed |= !a.Success; });
				this.SendOatCommand(":XGH#,#", (a) => { ha = a.Data; failed |= !a.Success; });
				this.SendOatCommand(":XGL#,#", (a) => { siderealTime = a.Data; failed |= !a.Success; });
				this.SendOatCommand(":XGN#,#", (a) => { network = a.Data; failed |= !a.Success; });
				if (editable)
				{
					this.SendOatCommand(":XGT#,#", (a) => { speed = a.Data; failed |= !a.Success; });
				}
				this.SendOatCommand(":XLGT#,#", (a) => { temperature = a.Success ? a.Data : "0"; failed |= !a.Success; allDone.Set(); });
				allDone.WaitOne(5000);

				if (!failed)
				{
					if (localTime.Length == 8)
					{
						ScopeTime = localTime;
						if (utcOffset.Length != 0)
						{
							ScopeTime += " T" + utcOffset + ":00";
						}
					}
					else
					{
						ScopeTime = "-";
					}

					if (localDate.Length == 8)
					{
						ScopeDate = localDate;
					}
					else
					{
						ScopeDate = "-";
					}

					if (siderealTime.Length == 6)
					{
						ScopeSiderealTime = $"{siderealTime.Substring(0, 2)}:{siderealTime.Substring(2, 2)}:{siderealTime.Substring(4)}";
					}
					else
					{
						ScopeSiderealTime = "-";
					}

					if (ha.Length == 6)
					{
						ScopePolarisHourAngle = $"{ha.Substring(0, 2)}:{ha.Substring(2, 2)}:{ha.Substring(4)}";
					}

					if (speed.Length > 0)
					{
						TrackingSpeed = float.Parse(speed, _oatCulture);
					}

					if (temperature != "0")
					{
						float temp = float.Parse(temperature, _oatCulture);
						int fahrenheit = (int)Math.Round(32.0 + (9.0 * temp / 5.0));
						ScopeTemperature = $"{temp:0.0}°C ({fahrenheit:0}°F)";
					}
					else
					{
						ScopeTemperature = "-";
					}

					if (network == "0,")
					{
						ScopeNetworkState = "Disabled";
						ScopeNetworkIPAddress = "N/A";
						ScopeNetworkSSID = "N/A";
					}
					else
					{
						var parts = network.Split(',');
						if (_firmwareVersion < 10873)
						{
							if (parts.Length > 4)
							{
								ScopeNetworkState = parts[1];
								ScopeNetworkIPAddress = parts[3];
								ScopeNetworkSSID = parts[4];
							}
							else
							{
								ScopeNetworkState = "-";
								ScopeNetworkIPAddress = "-";
								ScopeNetworkSSID = "-";
							}
						}
						else
						{
							if (parts.Length > 5)
							{
								ScopeNetworkState = parts[1] + ", " + parts[2];
								ScopeNetworkIPAddress = parts[4];
								ScopeNetworkSSID = parts[5];
							}
							else
							{
								ScopeNetworkState = "-";
								ScopeNetworkIPAddress = "-";
								ScopeNetworkSSID = "-";
							}
						}
					}
				}
			}
		}

		public void UpdateLocation()
		{
			float latitude = AppSettings.Instance.SiteLatitude;
			float longitude = AppSettings.Instance.SiteLongitude;

			ScopeLatitude = $"{Math.Abs(latitude):0.00}{(latitude < 0 ? "S" : "N")}";
			ScopeLongitude = $"{Math.Abs(longitude):0.00}{(longitude < 0 ? "W" : "E")}";
		}

		private async Task OnHome()
		{
			Log.WriteLine("Mount: Home requested");
			this.SendOatCommand(":hF#", (a) => { });

			// The next call actually blocks because Homeing is synchronous
			await UpdateStatus();

			await ReadHA();
		}

		private async Task OnPark()
		{
			if (ParkCommandString == "Park")
			{
				Log.WriteLine("Mount: Parking requested");
				this.SendOatCommand(":hP#", (a) =>
				{
					ParkCommandString = "Unpark";
				});
			}
			else
			{
				Log.WriteLine("Mount: Unparking requested");
				this.SendOatCommand(":hU#,n", (a) =>
				{
					ParkCommandString = "Park";
				});
			}

			// The next call actually blocks because Homeing is synchronous
			await UpdateStatus();
		}

		private bool IsSlewing(char dir)
		{
			if (dir == 'n') return IsSlewingNorth;
			if (dir == 'e') return IsSlewingEast;
			if (dir == 'w') return IsSlewingWest;
			if (dir == 's') return IsSlewingSouth;
			if (dir == 'a') return IsSlewingNorth | IsSlewingSouth | IsSlewingWest | IsSlewingEast;
			return false;
		}

		private async Task OnStopSlewing(char dir)
		{
			Log.WriteLine("CONTROL: Enter Stop slewing {0}", dir);

			// a/z - ALT up/down
			// l/r - AZ left/right
			// f/g - FOC in/out
			// n/s - DEC
			// w/e - RA
			// * - all motors
			var doneEvent = new AsyncAutoResetEvent();
			if ((dir != 'a') && (dir != 'z') && (dir != 'l') && (dir != 'r'))
			{
				// Don't handle AZ/ALT here
				if ((dir == 'f') || (dir == 'g'))
				{
					// Focus commands
					if (ScopeHasFOC)
					{
						_oatMount?.SendCommand(":FQ#", (a) => { doneEvent.Set(); });
					}
					else
					{
						doneEvent.Set();
					}
				}
				else
				{
					if (dir == '*')
					{
						// All motors
						if (ScopeHasFOC)
						{
							_oatMount?.SendCommand(":FQ#", (a) => { });
						}
						_oatMount?.SendCommand(":Q#", (a) => { doneEvent.Set(); });
					}
					else
					{
						// Slew commands
						_oatMount?.SendCommand($":Q{dir}#", (a) => { doneEvent.Set(); });
					}
					await RecalculatePointsPositions(true);
				}
			}
			else
			{
				doneEvent.Set();
			}

			await doneEvent.WaitAsync();
			Log.WriteLine("CONTROL: Exit Stop slewing {0}", dir);
		}

		private async Task OnStartSlewing(char dir)
		{
			Log.WriteLine("CONTROL: Enter Start slewing {0}", dir);

			float[] rateDistance = { 0, 0.25f, 2.0f, 7.5f, 30.0f };
			var doneEvent = new AsyncAutoResetEvent();
			float distance = rateDistance[SlewRate];
			// a/z - ALT up/down
			// l/r - AZ left/right
			// f/g - FOC in/out
			// n/s - DEC
			// w/e - RA
			char sign = (dir == 'a') || (dir == 'f') || (dir == 'l') ? '+' : '-';
			if ((dir == 'a') || (dir == 'z'))
			{
				_oatMount?.SendCommand(string.Format(_oatCulture, ":MAL{0}{1:0.0}#", sign, distance), (a) => { doneEvent.Set(); });
			}
			else if ((dir == 'l') || (dir == 'r'))
			{
				_oatMount?.SendCommand(string.Format(_oatCulture, ":MAZ{0}{1:0.0}#", sign, distance), (a) => { doneEvent.Set(); });
			}
			else if ((dir == 'f') || (dir == 'g'))
			{
				if (ScopeHasFOC)
				{
					_oatMount?.SendCommand(string.Format(_oatCulture, ":F{0}#", SlewRate), (a) => { });
					_oatMount?.SendCommand(string.Format(_oatCulture, ":F{0}#", sign), (a) => { doneEvent.Set(); });
				}
				else
				{
					doneEvent.Set();
				}
			}
			else
			{
				_oatMount?.SendCommand(string.Format(_oatCulture, ":M{0}#", dir), (a) => { doneEvent.Set(); });
			}
			await doneEvent.WaitAsync();

			Log.WriteLine("CONTROL: Exit Start slewing {0}", dir);
		}

		private async Task OnChangeSlewingState(string direction)
		{
			Log.WriteLine("CONTROL: Enter Change Slewing state to {0}", direction);
			if (direction != _lastDirection)
			{
				bool turnOn = direction[0] == '+';
				char dir = char.ToLower(direction[1]);
				if (turnOn)
				{
					await OnStartSlewing(dir);
				}
				else
				{
					await OnStopSlewing(dir);
				}
				_lastDirection = direction;
			}
			Log.WriteLine("CONTROL: Exit Change Slewing state to {0}", direction);
		}

		private async Task OnSlewToTarget()
		{
			var waitFor = new AsyncAutoResetEvent();
			_slewTargetValid = false;
			this.SendOatCommand(string.Format(_oatCulture, ":XGC{0:0.000}*{1:0.000}#,#", this._targetRA.TotalHours, this._targetDEC.TotalHours), (res) =>
			{
				if (res.Success)
				{
					var parts = res.Data.Split('|');
					_slewTargetRA = long.Parse(parts[0]);
					_slewTargetDEC = long.Parse(parts[1]);
					_slewTargetValid = true;
				}
				waitFor.Set();
			});
			await waitFor.WaitAsync();

			if (ShowDECLimits && _slewTargetValid)
			{
				float targetDegree = _slewTargetDEC / DECStepsPerDegree;
				bool upperLimitExceeded = targetDegree > DECStepperUpperLimit;
				bool lowerLimitExceeded = targetDegree < DECStepperLowerLimit;

				if (lowerLimitExceeded || upperLimitExceeded)
				{
					float overBy = lowerLimitExceeded ? DECStepperLowerLimit - targetDegree : targetDegree - DECStepperUpperLimit;
					var win = Application.Current.Windows.OfType<MetroWindow>().FirstOrDefault();
					var a = new MetroDialogSettings();
					a.AffirmativeButtonText = "Yes, slew anyway";
					a.NegativeButtonText = "No, don't slew";
					string violatedConstraint = lowerLimitExceeded ? "below the lower" : "above the upper";
					var result = await DialogManager.ShowMessageAsync(win, "DEC Limits Exceeded", $"This target is {violatedConstraint} currentyl set DEC limit by {overBy:0.0} degrees.\n\nSlew anyway?", MessageDialogStyle.AffirmativeAndNegative, a);

					if (result != MessageDialogResult.Affirmative)
					{
						return;
					}
				}
			}

			await _oatMount.Slew(new TelescopePosition(_targetRA.TotalHours, _targetDEC.TotalDegrees, Epoch.JNOW));
		}

		private async Task OnSyncToTarget()
		{
			await _oatMount.Sync(new TelescopePosition(_targetRA.TotalHours, _targetDEC.TotalDegrees, Epoch.JNOW));
		}

		private void OnSyncToCurrent()
		{
			_targetRA = new DayTime(_currentRA);
			_targetDEC = new Declination(_currentDEC);
			OnTargetChanged(0, 0);
			UpdateCurrentDisplay();
		}

		private void FloatToHMS(double val, out int h, out int m, out int s)
		{
			h = (int)Math.Floor(val);
			val = (val - h) * 60;
			m = (int)Math.Floor(val);
			val = (val - m) * 60;
			s = (int)Math.Round(val);
		}

		private async Task UpdateCurrentCoordinates()
		{
			int h, m, s;
			var pos = await _oatMount.GetPosition();
			FloatToHMS(pos.Declination, out h, out m, out s);
			_currentDEC.SetTime(h, m, s);

			FloatToHMS(pos.RightAscension, out h, out m, out s);
			_currentRA.SetTime(h, m, s);

			UpdateCurrentDisplay();
		}

		public void Disconnect()
		{
			if (_miniController != null)
			{
				_miniController.Close();
				_miniController = null;
			}

			if (_targetChooser != null)
			{
				_targetChooser.Close();
				_targetChooser = null;
			}

			if (MountConnected)
			{
				AppSettings.Instance.ShowDecLimits = ShowDECLimits;
				AppSettings.Instance.LowerDecLimit = DECStepperLowerLimit;
				AppSettings.Instance.UpperDecLimit = DECStepperUpperLimit;
				AppSettings.Instance.KeepMiniControlOnTop = KeepMiniControlOnTop;
				AppSettings.Instance.Save();
				MountConnected = false;
			}

			ShowDECLimits = false;

			if (_oatSimComm != null)
			{
				_oatSimComm.Stop();
				_oatSimComm = null;
			}

			if (_commHandler != null)
			{
				_commHandler.Disconnect();
				_commHandler = null;
			}

			ScopeName = string.Empty;
			ConnectionState = string.Empty;
			ScopeHardware = string.Empty;
			_oatMount = null;
			RequeryCommands();
		}

		public async Task UpdateInitialScopeValues()
		{
			try
			{
				var doneEvent = new AsyncAutoResetEvent();

				// Calculate current Local Sidereal Time
				await SetMountTimeData();

				Log.WriteLine("Mount: Getting current OAT position");
				await UpdateCurrentCoordinates();
				_targetRA = new DayTime(_currentRA);
				_targetDEC = new Declination(_currentDEC);
				Log.WriteLine("Mount: Current OAT position is RA: {0:00}:{1:00}:{2:00} and DEC: {3}{4:000}*{5:00}'{6:00}", CurrentRAHour, CurrentRAMinute, CurrentRASecond, CurrentDECSign, CurrentDECDegree, CurrentDECMinute, CurrentDECSecond);

				Log.WriteLine("Mount: Getting current OAT RA steps/degree...");
				this.SendOatCommand(":XGR#,#", (steps) =>
				{
					Log.WriteLine("Mount: Current RA steps/degree is {0}.", steps.Data);
					if (steps.Success)
					{
						RAStepsPerDegree = Math.Max(1, float.Parse(steps.Data, _oatCulture));
					}
				});

				Log.WriteLine("Mount: Getting current OAT DEC steps/degree...");
				this.SendOatCommand(":XGD#,#", (steps) =>
				{
					Log.WriteLine("Mount: Current DEC steps/degree is {0}. ", steps.Data);
					if (steps.Success)
					{
						SetDECStepsPerDegree(Math.Max(1, float.Parse(steps.Data, _oatCulture)));
					}
				});

				// We want this command to execute before we wait for the last one, since commands are executed sequentially.
				Log.WriteLine("Mount: Reading Current OAT HA...");
				await ReadHA();

				Log.WriteLine("Mount: Getting current OAT speed factor...");
				this.SendOatCommand(":XGS#,#", (speed) =>
				{
					Log.WriteLine("Mount: Current Speed factor is {0}...", speed.Data);
					if (speed.Success)
					{

						SpeedCalibrationFactor = float.Parse(speed.Data, _oatCulture);
						//OnPropertyChanged("SpeedCalibrationFactor");
						OnPropertyChanged("SpeedCalibrationFactorDisplay");
					}
					doneEvent.Set();
				});

				// Wait for RA Steps to be set before getting Tracking speed (it's used in remaining time calculation)
				await doneEvent.WaitAsync();
				Log.WriteLine("Mount: Getting current OAT tracking speed ...");
				this.SendOatCommand(":XGT#,#", (speed) =>
				{
					Log.WriteLine("Mount: Current Tracking Speed is {0}...", speed.Data);
					if (speed.Success)
					{
						TrackingSpeed = float.Parse(speed.Data, _oatCulture);
					}
					//OnPropertyChanged("TrackingSpeed");
					doneEvent.Set();
				});

				await doneEvent.WaitAsync();

				ShowDECLimits = AppSettings.Instance.ShowDecLimits;
				KeepMiniControlOnTop = AppSettings.Instance.KeepMiniControlOnTop;
				DECStepperLowerLimit = AppSettings.Instance.LowerDecLimit;
				DECStepperUpperLimit = AppSettings.Instance.UpperDecLimit;

				_connectedAt = DateTime.UtcNow;
				MountConnected = true;
				Log.WriteLine("Mount: Successfully connected and configured!");
			}
			catch (FormatException fex)
			{
				ScopeName = string.Empty;
				ScopeHardware = string.Empty;
				ConnectionState = string.Empty;
				Log.WriteLine("Mount: Failed to connect and configure OAT! {0}", fex.Message);
				MessageBox.Show("Connected to OpenAstroTracker, but protocol could not be established.\n\nIs the firmware compiled with DEBUG_LEVEL set to DEBUG_NONE?", "Protocol Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			catch (Exception ex)
			{
				ScopeName = string.Empty;
				ScopeHardware = string.Empty;
				ConnectionState = string.Empty;
				Log.WriteLine("Mount: Failed to connect and configure OAT! {0}", ex.Message);
				MessageBox.Show("Error trying to connect to OpenAstroTracker.\n\n" + ex.Message, "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private async void OnConnectToTelescope()
		{
			if (MountConnected)
			{
				Disconnect();
			}
			else
			{
				RequeryCommands();

				Log.WriteLine("Mount: Connect to OAT requested");

				if (await this.ChooseTelescope())
				{
					await RecalculatePointsPositions(true);
					OnSimulationClientConnect();
				}
			}
		}

		private async Task SetMountTimeData()
		{
			var utcNow = DateTime.UtcNow;
			var now = DateTime.Now;
			if (_firmwareVersion >= 10864)
			{
				Log.WriteLine("Mount: New FW: Current UTC is {0}. Sending to OAT.", utcNow);
				this.SendOatCommand(string.Format(_oatCulture, ":SL{0,2:00}:{1,2:00}:{2,2:00}#,n", now.Hour, now.Minute, now.Second), (a) => { });
				this.SendOatCommand(string.Format(_oatCulture, ":SC{0,2:00}/{1,2:00}/{2,2:00}#,##", now.Month, now.Day, now.Year - 2000), (a) => { });

				var utcOffset = Math.Round((now - utcNow).TotalHours);
				char sign = (utcOffset < 0) ? '-' : '+';
				this.SendOatCommand(string.Format(_oatCulture, ":SG{0}{1,2:00}#,n", sign, Math.Abs(utcOffset)), (a) => { });
			}
			else
			{
				var SiteLongitude = await _oatMount.GetSiteLongitude();

				var decimalTime = (utcNow.Hour + utcNow.Minute / 60.0) + (utcNow.Second / 3600.0);
				var jd = JulianDay(utcNow.Day, utcNow.Month, utcNow.Year, decimalTime);
				var lst = LM_Sidereal_Time(jd, SiteLongitude);
				var lstS = doubleToHMS(lst, "", "", "");

				Log.WriteLine("Mount: Old FW: Current LST is {0}. Sending to OAT.", doubleToHMS(lst, "h", "m", "s"));
				this.SendOatCommand($":SHL{doubleToHMS(lst, "", "", "")}#,n", (a) => { });
			}
		}

		private void OnRunPolarAlignment()
		{
			Log.WriteLine("Mount: Running Polar Alignment Wizard");
			DlgRunPolarAlignment dlg = new DlgRunPolarAlignment(this.SendOatCommand) { Owner = Application.Current.MainWindow, WindowStartupLocation = WindowStartupLocation.CenterOwner }; ;
			dlg.ShowDialog();
			Log.WriteLine("Mount: Polar Alignment Wizard completed");
		}

		private async Task OnRunDriftAlignment(int driftDuration)
		{
			_timerStatus.Stop();

			Log.WriteLine("Mount: Running Drift Alignment");

			DriftAlignStatus = "Drift Alignment running...";
			bool wasTracking = false;
			this.SendOatCommand(":GIT#,#", (tracking) =>
			{
				wasTracking = tracking.Data == "1";
			});

			IsTracking = false;
			DateTime startTime = DateTime.UtcNow;
			TimeSpan duration = TimeSpan.FromSeconds(2 * driftDuration + 2);
			await Task.Delay(200);

			try
			{
				var doneEvent = new AsyncAutoResetEvent();
				this.SendOatCommand(string.Format(_oatCulture, ":XD{0:000}#", driftDuration), (a) =>
				{
					IsDriftAligning = a.Success;
					doneEvent.Set();
				});
				doneEvent.WaitAsync();
				RequeryCommands();
			}
			finally
			{
				await Task.Run(() =>
				{
					DateTime endTime = startTime + duration;
					while (DateTime.UtcNow < endTime)
					{
						Thread.Sleep(50);
						float driftPhase = (float)((DateTime.UtcNow - startTime).TotalSeconds / duration.TotalSeconds);
						DriftPhase = driftPhase;
					}
				});
			}

			DriftAlignStatus = "Drift Alignment";
			IsDriftAligning = false;
			IsTracking = wasTracking;
			RequeryCommands();
			_timerStatus.Start();
			Log.WriteLine("Mount: Completed Drift Alignment");
		}

		private void RequeryCommands()
		{
			_connectScopeCommand.Requery();
			_slewToTargetCommand.Requery();
			_syncToTargetCommand.Requery();
			_syncToCurrentCommand.Requery();
			_changeSlewingStateCommand.Requery();
			_stopSlewingCommand.Requery();
			_homeCommand.Requery();
			_setHomeCommand.Requery();
			_parkCommand.Requery();
			_driftAlignCommand.Requery();
			_polarAlignCommand.Requery();
			_showLogFolderCommand.Requery();
			_showSettingsCommand.Requery();
			_showMiniControllerCommand.Requery();
			_chooseTargetCommand.Requery();
			_setDecLowerLimitCommand.Requery();
			_setDECHomeOffsetFromPowerOn.Requery();
			_setRAHomeOffsetCommand.Requery();
			_gotoDECHomeFromPowerOn.Requery();
			_autoHomeRACommand.Requery();
			_gotoDECParkBeforePowerOff.Requery();
			_focuserResetCommand.Requery();
			_customActionCommand.Requery();

			OnPropertyChanged("ConnectCommandString");
		}

		public async Task<Tuple<bool, string>> ConnectToOat(string device)
		{
			string message = string.Empty;
			_oatAddonStates.Clear();

			bool failed = false;
			string resultName = string.Empty;
			var doneEvent = new AsyncAutoResetEvent();

			ConnectionState = "Connecting. Opening port...";

			_commHandler = CommunicationHandlerFactory.ConnectToDevice(device);
			if (_commHandler != null)
			{
				if (_commHandler.Connect())
				{
					ConnectionState = "Connecting. Querying OAT...";
					_oatMount = new OatmealTelescopeCommandHandlers(_commHandler);

					for (int attempts = 0; attempts < 4; attempts++)
					{
						Log.WriteLine("Mount: Request OAT Firmware version (Attempt #{0})", attempts + 1);

						this.SendOatCommand("GVP#,#", (result) =>
						{
							if (!result.Success)
							{
								message = "Unable to communicate with OAT.";
								Log.WriteLine("Mount: {0} ({1})", message, result.StatusMessage);
								failed = true;
							}
							else
							{
								failed = false;
								resultName = result.Data;
								Log.WriteLine("Mount: Successfully communicated with OAT");
							}
							Log.WriteLine("Mount: Setting signal");
							doneEvent.Set();
						});

						Log.WriteLine("Mount: Request Sent. Awaiting signal. (Attempt #{0})", attempts + 1);
						await doneEvent.WaitAsync();
						Log.WriteLine("Mount: Signal received. (Attempt #{0})", attempts + 1);
						if (!failed)
						{
							break;
						}
					}
					if (failed)
					{
						_commHandler?.Disconnect();
					}
				}
				else
				{
					failed = true;
				}
			}
			else
			{
				failed = true;
			}


			if (failed)
			{
				ConnectionState = string.Empty;
				return Tuple.Create(false, message);
			}

			ConnectionState = $"Connected via {_commHandler.Name}";

			await Task.Delay(200);

			Log.WriteLine("Mount: Connected to OAT. Requesting firmware version..");
			this.SendOatCommand("GVN#,#", (resultNr) =>
			{
				if (resultNr.Success)
				{
					if (resultNr.Data.StartsWith("["))
					{
						message = "OAT likely has debug logging enabled. Check firmware.";
						Log.WriteLine("Mount: {0} {1}", message, resultNr.Data);
						failed = true;
					}
					else
					{
						ScopeVersion = resultNr.Data;
						ScopeName = $"{resultName} {resultNr.Data}";
						var versionNumbers = resultNr.Data.Substring(1).Split(".".ToCharArray());
						if (versionNumbers.Length != 3)
						{
							message = $"Unrecognizable firmware version '{resultNr.Data}'";
							Log.WriteLine("Mount: {0}", message);
							failed = true;
						}
						else
						{
							try
							{
								FirmwareVersion = long.Parse(versionNumbers[0]) * 10000L + long.Parse(versionNumbers[1]) * 100L + long.Parse(versionNumbers[2]);
							}
							catch
							{
								message = $"Unable to parse firmware version '{resultNr.Data}'";
								Log.WriteLine("Mount: {0}", message);
								failed = true;
							}
						}
					}
				}
				else
				{
					message = "Communication failed...";
					failed = true;
				}

				doneEvent.Set();
			});

			await doneEvent.WaitAsync();
			if (failed)
			{
				return Tuple.Create(false, message);
			}

			var doneEvent2 = new AsyncAutoResetEvent();
			string hwData = string.Empty;

			this.SendOatCommand("XGM#,#", (hardware) =>
			{
				Log.WriteLine("Mount: Hardware is {0}", hardware.Data);
				hwData = hardware.Data;
				failed |= !hardware.Success;
				doneEvent2.Set();
			});
			await doneEvent2.WaitAsync();

			if (failed)
			{
				return Tuple.Create(false, "Communication failed.");
			}

			var hwParts = hwData.Split(',');
			var raParts = hwParts[1].Split('|');
			var decParts = hwParts[2].Split('|');
			ScopeHardware = $"{hwParts[0]} board    RA {raParts[0]}, {raParts[1]}T    DEC {decParts[0]}, {decParts[1]}T";
			ScopeBoard = hwParts[0];
			ScopeRAStepper = $"{raParts[0]}, {raParts[1]}T";
			ScopeDECStepper = $"{decParts[0]}, {decParts[1]}T";
			ScopeHasALT = false;
			ScopeHasAZ = false;
			ScopeHasFOC = false;
			ScopeHasHSAH = false;
			ScopeFeatures = "";
			ScopeDisplay = "None";

			if (_firmwareVersion > 10875)
			{
				var doneEvent3 = new AsyncAutoResetEvent();
				string stepperData = string.Empty;
				this.SendOatCommand("XGMS#,#", (stepper) =>
				{
					Log.WriteLine("Mount: Stepper info is {0}", stepper.Data);
					failed |= !stepper.Success;
					stepperData = stepper.Data;
					doneEvent3.Set();
				});
				await doneEvent3.WaitAsync();
				if (failed)
				{
					return Tuple.Create(false, "Communication failed...");
				}

				var steppers = stepperData.Split('|');
				var raStepper = steppers[0].Split(',');
				var decStepper = steppers[1].Split(',');
				switch (raStepper[0])
				{
					case "U": ScopeRADriver = "ULN2003"; break;
					case "TU": ScopeRADriver = "TMC2209 UART"; break;
					case "TS": ScopeRADriver = "TMC2209 Standalone"; break;
					case "A": ScopeRADriver = "A4988 Generic"; break;
				}

				switch (decStepper[0])
				{
					case "U": ScopeDECDriver = "ULN2003"; break;
					case "TU": ScopeDECDriver = "TMC2209 UART"; break;
					case "TS": ScopeDECDriver = "TMC2209 Standalone"; break;
					case "A": ScopeDECDriver = "A4988 Generic"; break;
				}

				ScopeRASlewMS = raStepper[1];
				ScopeRATrackMS = raStepper[2];
				ScopeDECSlewMS = decStepper[1];
				ScopeDECGuideMS = decStepper[2];
			}

			for (int i = 3; i < hwParts.Length; i++)
			{
				_oatAddonStates.Add(hwParts[i]);
				if (hwParts[i] == "GPS")
				{
					ScopeFeatures += "GPS, ";
				}
				else if (hwParts[i] == "AUTO_AZ_ALT")
				{
					ScopeFeatures += "AutoPA, ";
					ScopeHasALT = true;
					ScopeHasAZ = true;
				}
				else if (hwParts[i] == "AUTO_ALT")
				{
					ScopeFeatures += "MotorALT, ";
					ScopeHasALT = true;
				}
				else if (hwParts[i] == "AUTO_AZ")
				{
					ScopeFeatures += "MotorAZ, ";
					ScopeHasAZ = true;
				}
				else if (hwParts[i] == "GYRO")
				{
					ScopeFeatures += "Digital Level, ";
				}
				else if (hwParts[i] == "LCD_KEYPAD")
				{
					ScopeDisplay = "16x2 LCD (w/ buttons) ";
				}
				else if (hwParts[i] == "LCD_I2C_MCP23008")
				{
					ScopeDisplay = "LCD (I2C MCP23008) ";
				}
				else if (hwParts[i] == "LCD_I2C_MCP23017")
				{
					ScopeDisplay = "LCD (I2C MCP23017)";
				}
				else if (hwParts[i] == "LCD_JOY_I2C_SSD1306")
				{
					ScopeDisplay = "Pixel OLED (SSD1306)";
				}
				else if (hwParts[i] == "FOC")
				{
					ScopeHasFOC = true;
					ScopeFeatures += "Focuser, ";
				}
				else if (hwParts[i] == "HSAH")
				{
					ScopeHasHSAH = true;
					ScopeFeatures += "RA Auto Home, ";
				}
			}
			if (string.IsNullOrEmpty(ScopeFeatures))
			{
				ScopeFeatures = "No addons";
			}
			else
			{
				ScopeFeatures = ScopeFeatures.Substring(0, ScopeFeatures.Length - 2);
			}

			return Tuple.Create(true, string.Empty);
		}

		public bool IsAddonSupported(string addon)
		{
			return _oatAddonStates.Contains(addon);
		}

		public IList<string> Addons
		{
			get
			{
				return _oatAddonStates;
			}
		}

		private async Task<bool> ChooseTelescope()
		{
			_serialBaudRate = AppSettings.Instance.BaudRate;

			var dlg = new DlgChooseOat(this, SendOatCommand) { Owner = Application.Current.MainWindow, WindowStartupLocation = WindowStartupLocation.CenterOwner };

			Log.WriteLine("Mount: Showing OAT comms Chooser Wizard");
			dlg.ShowDialog();

			_timerStatus.Stop();

			if (dlg.Result == true)
			{
				_inStartup = true;
				UpdateLocation();
				Log.WriteLine("SYSTEM: OAT Connected!");
				await UpdateInitialScopeValues();

				bool setHome = false;
				bool abortHoming = false;

				if (dlg.RunDECOffsetHoming && (AppSettings.Instance.DECHomeOffset != 0))
				{
					Log.WriteLine("DEC unparking starting");
					var doneEvent = new AsyncAutoResetEvent();
					_oatMount.SendCommand($":MXd{AppSettings.Instance.DECHomeOffset}#,n", (a) => { doneEvent.Set(); });
					await doneEvent.WaitAsync();

					var dlgWait = new DlgWaitForGXState("Unparking DEC...", this, SendOatCommand, (s) =>
					{
						if (s == null) return false;
						Log.WriteLine("DEC Unpark state: ", s[0]);
						return (s[0] != "Idle") && (s[0] != "Tracking");
					})
					{
						Owner = Application.Current.MainWindow,
						WindowStartupLocation = WindowStartupLocation.CenterOwner
					};

					dlgWait.ShowDialog();
					Log.WriteLine("DEC unparking complete");
					setHome = dlgWait.DialogResult.Value;
					abortHoming = !dlgWait.DialogResult.Value;
					if (!dlgWait.DialogResult.Value)
					{
						_oatMount.SendCommand(":Q#", _ => { });
					}
				}

				if ((FirmwareVersion > 10935) && dlg.RunRAAutoHoming && ScopeHasHSAH && !abortHoming)
				{
					Log.WriteLine("RA Auto Home starting");
					var statuses = new List<string>();
					var doneEvent = new AsyncAutoResetEvent();
					// The MHRR command actually returns 0 or 1, but we ignore it, so that we can monitor progress
					_oatMount.SendCommand($":MHRR#", (a) => { doneEvent.Set(); });
					await doneEvent.WaitAsync();

					// Open the GX monitoring dialog
					var dlgWait = new DlgWaitForGXState("Homing RA...", this, SendOatCommand, (s) =>
					{
						if (s == null) return false;
						statuses.Add(s[0]);
						//Log.WriteLine("RA Homing state: ", s[0]);
						return (s[0] != "Idle") && (s[0] != "Tracking");
					})
					{
						Owner = Application.Current.MainWindow,
						WindowStartupLocation = WindowStartupLocation.CenterOwner
					};

					dlgWait.ShowDialog();
					Log.WriteLine("RA Homing complete. Statuses: " + string.Join(", ", statuses));
					setHome = dlgWait.DialogResult.Value;
					if (!dlgWait.DialogResult.Value)
					{
						_oatMount.SendCommand(":Q#", _ => { });
					}
				}

				if (setHome)
				{
					await OnSetHome();
				}

				_timerStatus.Start();

				_inStartup = false;
				return true;
			}
			else if (dlg.Result == null)
			{
				Log.WriteLine("Mount: Unable to connect");
				string extraMessage = "Is something else connected?";
				if (Process.GetProcesses().FirstOrDefault(d => d.ProcessName.Contains("ASCOM.OpenAstroTracker")) != null)
				{
					extraMessage = "Another process is connected via ASCOM.";
				}
				MessageBox.Show("Cannot connect to mount. " + extraMessage, "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}

			RequeryCommands();
			Log.WriteLine("Mount: Chooser cancelled");
			ScopeName = string.Empty;
			ScopeHardware = string.Empty;
			_oatAddonStates.Clear();
			if (_commHandler != null)
			{
				_commHandler.Disconnect();
			}

			_oatMount = null;
			_commHandler = null;

			return false;
		}

		private void SendOatCommand(string command, Action<CommandResponse> response)
		{
			if (_oatMount == null)
			{
				response(new CommandResponse("", false, "Disconnected.."));
			}
			else
			{
				_oatMount.SendCommand(command, response);
			}
		}


		// Adjust the given number by the given adjustment, wrap around the limits.
		// Limits are inclusive, so they represent the lowest and highest valid number.
		private int AdjustWrap(int current, int adjustBy, int minVal, int maxVal)
		{
			current += adjustBy;
			if (current > maxVal) current -= (maxVal + 1 - minVal);
			if (current < minVal) current += (maxVal + 1 - minVal);
			return current;
		}

		// Adjust the given number by the given adjustment, clamping to the limits.
		// Limits are inclusive, so they represent the lowest and highest valid number.
		private int AdjustClamp(int current, int adjustBy, int minVal, int maxVal)
		{
			current += adjustBy;
			if (current > maxVal) current = maxVal;
			if (current < minVal) current = minVal;
			return current;
		}

		public void OnAdjustTarget(string command)
		{
			int inc = command[2] == '-' ? -1 : 1;
			char comp = command[1];
			switch (command[0])
			{
				case 'R':
					if (comp == 'H') _targetRA.AddHours(inc);
					else if (comp == 'M') _targetRA.AddMinutes(inc);
					else if (comp == 'S') _targetRA.AddSeconds(inc);
					else { throw new ArgumentException("Invalid RA component!"); }
					UpdateTargetDisplay(true);
					break;
				case 'D':
					if (comp == 'D') _targetDEC.AddDegrees(inc);
					else if (comp == 'M') _targetDEC.AddMinutes(inc);
					else if (comp == 'S') _targetDEC.AddSeconds(inc);
					else { throw new ArgumentException("Invalid DEC component!"); }
					UpdateTargetDisplay(true);
					break;
			}
		}

		// Fraction
		private double Frac(double x)
		{
			x = x - Math.Floor(x);
			if (x < 0) x = x + 1.0;
			return x;
		}

		// Get the Julian Day as double
		private double JulianDay(int dat, int month, int year, double u)
		{
			if (month <= 2)
			{
				month = month + 12;
				year = year - 1;
			}
			var JD = Math.Floor(365.25 * (year + 4716.0)) + Math.Floor(30.6001 * (month + 1)) + dat - 13.0 - 1524.5 + u / 24.0;
			return JD;
		}

		// Calculate Local Sidereal Time
		// Reference https://greenbankobservatory.org/education/great-resources/lst-clock/
		private double GM_Sidereal_Time(double jd)
		{
			double t_eph, ut, MJD0, MJD;

			MJD = jd - 2400000.5;
			MJD0 = Math.Floor(MJD);
			ut = (MJD - MJD0) * 24.0;
			t_eph = (MJD0 - 51544.5) / 36525.0;
			return 6.697374558 + 1.0027379093 * ut + (8640184.812866 + (0.093104 - 0.0000062 * t_eph) * t_eph) * t_eph / 3600.0;
		}

		private double LM_Sidereal_Time(double jd, double longitude)
		{
			var GMST = GM_Sidereal_Time(jd);
			var LMST = 24.0 * Frac((GMST + longitude / 15.0) / 24.0);
			return LMST;
		}

		// Convert decimal time to HH:MM:SS
		private string doubleToHMS(double time, string delimiter1, string delimiter2, string delimiter3)
		{
			var h = Math.Floor(time);
			var min = Math.Floor(60.0 * Frac(time));
			var secs = Math.Floor(60.0 * (60.0 * Frac(time) - min));

			var hs = string.Format(_oatCulture, "{0:00}", h);
			var ms = string.Format(_oatCulture, "{0:00}", min);
			var ss = string.Format(_oatCulture, "{0:00}", secs);

			string res = hs + delimiter1 + ms + delimiter2 + ss + delimiter3;

			return res;
		}

		public void SetFocusTarget(string uiElement)
		{
			_keyboardFocus = uiElement;
			Log.WriteLine("Keyboard focus is {0}", _keyboardFocus);
		}

		public ICommand ArrowCommand { get { return _arrowCommand; } }
		public ICommand ConnectScopeCommand { get { return _connectScopeCommand; } }
		public ICommand SlewToTargetCommand { get { return _slewToTargetCommand; } }
		public ICommand SyncToTargetCommand { get { return _syncToTargetCommand; } }
		public ICommand SyncToCurrentCommand { get { return _syncToCurrentCommand; } }
		public ICommand ChangeSlewingStateCommand { get { return _changeSlewingStateCommand; } }
		public ICommand StopSlewingCommand { get { return _stopSlewingCommand; } }
		public ICommand HomeCommand { get { return _homeCommand; } }
		public ICommand SetHomeCommand { get { return _setHomeCommand; } }
		public ICommand ParkCommand { get { return _parkCommand; } }
		public ICommand DriftAlignCommand { get { return _driftAlignCommand; } }
		public ICommand PolarAlignCommand { get { return _polarAlignCommand; } }
		public ICommand ShowLogFolderCommand { get { return _showLogFolderCommand; } }
		public ICommand ShowSettingsCommand { get { return _showSettingsCommand; } }
		public ICommand ShowMiniControllerCommand { get { return _showMiniControllerCommand; } }
		public ICommand FactoryResetCommand { get { return _factoryResetCommand; } }
		public ICommand StartChangingCommand { get { return _startChangingCommand; } }
		public ICommand ChooseTargetCommand { get { return _chooseTargetCommand; } }
		public ICommand SetDecLowerLimitCommand { get { return _setDecLowerLimitCommand; } }
		public ICommand SetDECHomeOffsetFromPowerOnCommand { get { return _setDECHomeOffsetFromPowerOn; } }
		public ICommand SetRAHomeOffsetCommand { get { return _setRAHomeOffsetCommand; } }
		public ICommand GotoDECHomeFromPowerOnCommand { get { return _gotoDECHomeFromPowerOn; } }
		public ICommand AutoHomeRACommand { get { return _autoHomeRACommand; } }
		public ICommand GotoDECParkBeforePowerOffCommand { get { return _gotoDECParkBeforePowerOff; } }
		public ICommand FocuserResetCommand { get { return _focuserResetCommand; } }
		public ICommand CustomActionCommand { get { return _customActionCommand; } }

		public double TargetRATotalHours
		{
			get { return _targetRA.TotalHours; }
		}

		/// <summary>
		/// Gets or sets the RAHour
		/// </summary>
		public int TargetRAHour
		{
			get { return _targetRA.Hours; }
			set { SetPropertyValue(value, () => _targetRA.Hours, _targetRA.ChangeHour, OnTargetChanged); }
		}

		/// <summary>
		/// Gets or sets the RAMinute
		/// </summary>
		public int TargetRAMinute
		{
			get { return _targetRA.Minutes; }
			set { SetPropertyValue(value, () => _targetRA.Minutes, _targetRA.ChangeMinute, OnTargetChanged); }
		}

		/// <summary>
		/// Gets or sets the RASecond
		/// </summary>
		public int TargetRASecond
		{
			get { return _targetRA.Seconds; }
			set { SetPropertyValue(value, () => _targetRA.Seconds, _targetRA.ChangeSecond, OnTargetChanged); }
		}

		public double RAStepperTargetHours
		{
			get
			{
				return _raStepperTargetHours;
			}

			set
			{
				if (value != _raStepperTargetHours)
				{
					_raStepperTargetHours = value;
					OnPropertyChanged("RAStepperTargetHours");
				}
			}
		}

		private void OnTargetChanged(int arg1, int arg2)
		{
			UpdateTargetDisplay(false);

			this.SendOatCommand(string.Format(_oatCulture, ":XGC{0:0.000}*{1:0.000}#,#", TargetRATotalHours, TargetDECTotalHours), (res) =>
			{
				if (res.Success)
				{
					var parts = res.Data.Split('|');
					RAStepperTargetHours = long.Parse(parts[0]) / RAStepsPerDegree / 15.0;
					DECStepperTargetDegrees = long.Parse(parts[1]) / DECStepsPerDegree;
				}
			});
		}

		public double TargetDECTotalHours
		{
			get { return _targetDEC.TotalDegrees; }
		}

		public double DECStepperTargetDegrees
		{
			get
			{
				return _decStepperTargetDegrees;
			}

			set
			{
				if (value != _decStepperTargetDegrees)
				{
					_decStepperTargetDegrees = value;
					OnPropertyChanged("DECStepperTargetDegrees");
				}
			}
		}

		/// <summary>
		/// Gets or sets the DECDegree
		/// </summary>
		public int TargetDECDegree
		{
			get { return _targetDEC.Degrees; }
			set { SetPropertyValue(value, () => _targetDEC.Degrees, _targetDEC.ChangeDegree, OnTargetChanged); }
		}

		public string TargetDECSign
		{
			get { return _targetDEC.TotalSeconds < 0 ? "-" : "+"; }
		}

		public string CurrentDECSign
		{
			get { return _currentDEC.TotalSeconds < 0 ? "-" : "+"; }
		}

		/// <summary>
		/// Gets or sets the DECMinute
		/// </summary>
		public int TargetDECMinute
		{
			get { return _targetDEC.Minutes; }
			set { SetPropertyValue(value, () => _targetDEC.Minutes, _targetDEC.ChangeMinute, OnTargetChanged); }
		}

		/// <summary>
		/// Gets or sets the DECSecond
		/// </summary>
		public int TargetDECSecond
		{
			get { return _targetDEC.Seconds; }
			set { SetPropertyValue(value, () => _targetDEC.Seconds, _targetDEC.ChangeSecond, OnTargetChanged); }
		}

		public double CurrentRATotalHours
		{
			get { return _currentRA.TotalHours; }
		}

		public double CurrentDECTotalHours
		{
			get { return _currentDEC.TotalDegrees; }
		}

		/// <summary>
		/// Gets or sets the RAHour
		/// </summary>
		public int CurrentRAHour
		{
			get { return _currentRA.Hours; }
			set { SetPropertyValue(value, () => _currentRA.Hours, _currentRA.ChangeHour, OnRaOrDecChanged); }
		}

		private void OnRaOrDecChanged(int v1, int v2)
		{
			OnPropertyChanged("CurrentRAString");
			OnPropertyChanged("CurrentDECString");
			OnPropertyChanged("CurrentDECSign");

			Log.WriteLine("Mount: Steppers are at {0}, {1}", RAStepper, DECStepper);
			_pointsOfInterest.CalcDistancesFrom(CurrentRATotalHours, CurrentDECTotalHours, RAStepper, DECStepper);

			_pointsOfInterest.SortBy("Distance");
			_pointsOfInterest = new PointsOfInterest(_pointsOfInterest.ToList());
			OnPropertyChanged("AvailablePointsOfInterest");
		}

		/// <summary>
		/// Gets or sets the RAMinute
		/// </summary>
		public int CurrentRAMinute
		{
			get { return _currentRA.Minutes; }
			set { SetPropertyValue(value, () => _currentRA.Minutes, _currentRA.ChangeMinute, OnRaOrDecChanged); }
		}

		/// <summary>
		/// Gets or sets the RASecond
		/// </summary>
		public int CurrentRASecond
		{
			get { return _currentRA.Seconds; }
			set { SetPropertyValue(value, () => _currentRA.Seconds, _currentRA.ChangeSecond, OnRaOrDecChanged); }
		}

		public string CurrentRAString
		{
			get { return $"{CurrentRAHour:00}h{CurrentRAMinute:00}m{CurrentRASecond:00}s"; }
		}

		public string CurrentDECString
		{
			get { return $"{(_currentDEC.TotalSeconds < 0 ? "-" : "+")}{_currentDEC.Degrees:00}°{_currentDEC.Minutes:00}'{_currentDEC.Seconds:00}"; }
		}

		/// <summary>
		/// Gets or sets the DECDegree
		/// </summary>
		public int CurrentDECDegree
		{
			get { return _currentDEC.Degrees; }
			set { SetPropertyValue(value, () => _currentDEC.Degrees, _currentDEC.ChangeDegree, OnRaOrDecChanged); }
		}

		/// <summary>
		/// Gets or sets the DECMinute
		/// </summary>
		public int CurrentDECMinute
		{
			get { return _currentDEC.Minutes; }
			set { SetPropertyValue(value, () => _currentDEC.Minutes, _currentDEC.ChangeMinute, OnRaOrDecChanged); }
		}

		/// <summary>
		/// Gets or sets the DECSecond
		/// </summary>
		public int CurrentDECSecond
		{
			get { return _currentDEC.Seconds; }
			set { SetPropertyValue(value, () => _currentDEC.Seconds, _currentDEC.ChangeSecond, OnRaOrDecChanged); }
		}

		/// <summary>
		/// Gets or sets the TRK Stepper position
		/// </summary>
		public long TrkStepper
		{
			get { return _trkStepper; }
			set { SetPropertyValue(ref _trkStepper, value, OnRAPosChanged); }
		}

		/// <summary>
		/// Gets or sets the FOC Stepper position
		/// </summary>
		public long FocStepper
		{
			get { return _focStepper; }
			set { SetPropertyValue(ref _focStepper, value); }
		}

		/// <summary>
		/// Gets or sets the RA Stepper position
		/// </summary>
		public long RAStepper
		{
			get { return _raStepper; }
			set { SetPropertyValue(ref _raStepper, value, OnRAPosChanged); }
		}

		private void OnRAPosChanged(long a, long b)
		{
			if (long.TryParse(ScopeRATrackMS, out b))
			{
				var raTrackMs = long.Parse(ScopeRATrackMS);
				var raSlewMs = long.Parse(ScopeRASlewMS);
				long raPos = RAStepper + raSlewMs * TrkStepper / raTrackMs;
				var stepLimit = 105 * RAStepsPerDegree; // 105 degrees until the ring falls off the bearings

				int raStepsLeft = (int)Math.Round(stepLimit - raPos);
				double secondsLeft = (3600.0 / 15.0) * raStepsLeft / (RAStepsPerDegree * SpeedCalibrationFactor);
				RemainingRATime = TimeSpan.FromSeconds(secondsLeft).ToString("%h'h '%m'm'");
			}
			else
			{
				RemainingRATime = "-";
			}

			OnPropertyChanged("RAStepperHours");
		}

		public float RAStepperHours
		{
			get
			{
				bool valid = long.TryParse(ScopeRATrackMS, out long raTrackMs);
				valid &= long.TryParse(ScopeRASlewMS, out long raSlewMs);
				if (valid)
				{
					float trkStepsInRaMS = 1.0f * _trkStepper / (1.0f * raTrackMs / raSlewMs);
					return (trkStepsInRaMS + _raStepper) / RAStepsPerDegree / 15.0f;
				}
				return _raStepper / RAStepsPerDegree / 15.0f;
			}
		}

		/// <summary>
		/// Gets or sets the RAStepper position
		/// </summary>
		public float RAStepperMinimum
		{
			get { return _raStepperMinimum; }
			set { SetPropertyValue(ref _raStepperMinimum, value); }
		}

		/// <summary>
		/// Gets or sets the RA Stepper position
		/// </summary>
		public float RAStepperMaximum
		{
			get { return _raStepperMaximum; }
			set { SetPropertyValue(ref _raStepperMaximum, value); }
		}


		public string DECStepperTicks
		{
			get { return _decStepperTicks; }
			set { SetPropertyValue(ref _decStepperTicks, value); }
		}

		/// <summary>
		/// Gets or sets the DEC Stepper position
		/// </summary>
		public long DECStepper
		{
			get { return _decStepper; }
			set { SetPropertyValue(ref _decStepper, value); OnPropertyChanged("DECStepperDegrees"); }
		}

		public float DECStepperDegrees
		{
			get { return _decStepper / DECStepsPerDegree; }
		}

		/// <summary>
		/// Gets or sets the DEC Stepper position
		/// </summary>
		public float DECStepperMinimum
		{
			get { return _decStepperMinimum; }
			set { SetPropertyValue(ref _decStepperMinimum, value); }
		}

		/// <summary>
		/// Gets or sets the DEC Stepper position
		/// </summary>
		public float DECStepperMaximum
		{
			get { return _decStepperMaximum; }
			set { SetPropertyValue(ref _decStepperMaximum, value); }
		}

		public float DECStepperLowerLimit
		{
			get { return _decStepperLowerLimit; }
			set { SetPropertyValue(ref _decStepperLowerLimit, value, OnDECStepperLimitsChanged); }
		}

		public float DECStepperUpperLimit
		{
			get { return _decStepperUpperLimit; }
			set { SetPropertyValue(ref _decStepperUpperLimit, value, OnDECStepperLimitsChanged); }
		}

		private void OnDECStepperLimitsChanged(float arg1, float arg2)
		{
			_pointsOfInterest.SetDecLowStepLimit((long)Math.Round(DECStepperLowerLimit * DECStepsPerDegree));
			_pointsOfInterest.SetDecHighStepLimit((long)Math.Round(DECStepperUpperLimit * DECStepsPerDegree));
		}


		/// <summary>
		/// Gets or sets the DECSecond
		/// </summary>
		public double SpeedCalibrationFactor
		{
			get { return _speed; }
			set { SetPropertyValue(ref _speed, value, OnSpeedFactorChanged); }
		}

		private void OnSpeedFactorChanged(double oldVal, double newVal)
		{
			this.SendOatCommand(string.Format(_oatCulture, ":XSS{0:0.0000}#", newVal), (a) => { });
		}

		public long SpeedCalibrationFactorEdit
		{
			get { return _speedEdit; }
			set
			{
				if (_speedEdit != value)
				{
					_speedEdit = value;
					OnPropertyChanged("SpeedCalibrationFactorDisplay");
				}
			}
		}

		public string SpeedCalibrationFactorDisplay
		{
			get { return $"{1.0 + (_speedEdit / 10000.0):0.0000}"; }
		}

		public string ConnectedTime
		{
			get
			{
				TimeSpan connected = DateTime.UtcNow - _connectedAt;
				if (connected.TotalMinutes < 1)
				{
					return $"{connected.Seconds}s";
				}

				if (connected.TotalMinutes < 60)
				{
					return $"{connected.Minutes:00}m {connected.Seconds:00}s";
				}

				return $"{connected.Days * 24 + connected.Hours}h {connected.Minutes:00}m {connected.Seconds:00}s";
			}
		}

		public float RAStepsPerDegreeEdit
		{
			get { return _raStepsPerDegreeEdit; }
			set { SetPropertyValue(ref _raStepsPerDegreeEdit, value); }
		}

		/// <summary>
		/// Gets or sets the RA steps per degree
		/// </summary>
		public float RAStepsPerDegree
		{
			get { return _raStepsPerDegree; }
			set { SetPropertyValue(ref _raStepsPerDegree, value, OnRAStepsChanged); }
		}

		private void OnRAStepsChanged(float oldVal, float newVal)
		{
			this.SendOatCommand(string.Format(_oatCulture, ":XSR{0:0.0}#", newVal), (a) => { });
		}

		public float DECStepsPerDegreeEdit
		{
			get { return _decStepsPerDegreeEdit; }
			set { SetPropertyValue(ref _decStepsPerDegreeEdit, value); }
		}

		/// <summary>
		/// Gets or sets the DEC steps per degree
		/// </summary>
		public float DECStepsPerDegree
		{
			get { return _decStepsPerDegree; }
			set { SetPropertyValue(ref _decStepsPerDegree, value, OnDECStepsChanged); }
		}

		private void OnDECStepsChanged(float oldVal, float newVal)
		{
			this.SendOatCommand(string.Format(_oatCulture, ":XSD{0:0.0}#", newVal), (a) => { });
		}

		public bool DisplaySlewProgress
		{
			get { return _slewInProgress; }
			set { SetPropertyValue(ref _slewInProgress, value); }
		}

		public float RASlewProgress
		{
			get
			{
				if (_slewTargetRA == _slewStartRA) return 1.0f;
				if (_firmwareVersion > 10900)
				{
					float val = 1.0f * Math.Abs(RAStepper - _slewStartRA) / Math.Abs(_slewTargetRA - _slewStartRA);
					Log.WriteLine($"RA Progress: {val * 100:0}% . Total is {_slewTargetRA - _slewStartRA}, current is {RAStepper - _slewStartRA}");
					return val;
				}
				return 1.0f * (_currentRA.TotalSeconds - _slewStartRA) / (_slewTargetRA - _slewStartRA);
			}
		}

		public float DECSlewProgress
		{
			get
			{
				if (_slewTargetDEC == _slewStartDEC) return 1.0f;
				if (_firmwareVersion > 10900)
				{
					var val = 1.0f * Math.Abs(DECStepper - _slewStartDEC) / Math.Abs(_slewTargetDEC - _slewStartDEC);
					Log.WriteLine($"DEC Progress: {val * 100:0}% . Total is {_slewTargetDEC - _slewStartDEC}, current is {DECStepper - _slewStartDEC}");
					return val;
				}
				return 1.0f * (_currentDEC.TotalSeconds - _slewStartDEC) / (_slewTargetDEC - _slewStartDEC);
			}
		}


		public bool MountConnected
		{
			get { return _connected; }
			set { SetPropertyValue(ref _connected, value, MountConnectedChanged); }
		}

		private void MountConnectedChanged(bool oldVal, bool newVal)
		{
			RequeryCommands();
			OnPropertyChanged("ConnectCommandString");
		}

		/// <summary>
		/// Gets or sets the name of the scope
		/// </summary>
		public string ScopeName
		{
			get { return _scopeName; }
			set { SetPropertyValue(ref _scopeName, value); }
		}

		/// <summary>
		/// Gets or sets the version of the scope firmware
		/// </summary>
		public string ScopeVersion
		{
			get { return _scopeVersion; }
			set { SetPropertyValue(ref _scopeVersion, value); }
		}

		/// <summary>
		/// Gets or sets the hardware config of the scope
		/// </summary>
		public string ScopeHardware
		{
			get { return _scopeHardware; }
			set { SetPropertyValue(ref _scopeHardware, value); }
		}

		public string ConnectionState
		{
			get { return _connectionState; }
			set { SetPropertyValue(ref _connectionState, value); }
		}

		/// <summary>
		/// Gets or sets the type of RA stepper of the scope 
		/// </summary>
		public string ScopeRAStepper
		{
			get { return _scopeRAStepper; }
			set { SetPropertyValue(ref _scopeRAStepper, value); }
		}

		public string ScopeRADriver
		{
			get { return _scopeRADriver; }
			set { SetPropertyValue(ref _scopeRADriver, value); }
		}

		/// <summary>
		/// Gets or sets the type of DEC stepper of the scope 
		/// </summary>
		public string ScopeDECStepper
		{
			get { return _scopeDECStepper; }
			set { SetPropertyValue(ref _scopeDECStepper, value); }
		}

		public string ScopeDECDriver
		{
			get { return _scopeDECDriver; }
			set { SetPropertyValue(ref _scopeDECDriver, value); }
		}

		public string ScopeRASlewMS
		{
			get { return _scopeRASlewMS; }
			set { SetPropertyValue(ref _scopeRASlewMS, value); }
		}

		public string ScopeRATrackMS
		{
			get { return _scopeRATrackMS; }
			set { SetPropertyValue(ref _scopeRATrackMS, value); }
		}

		public string ScopeDECSlewMS
		{
			get { return _scopeDECSlewMS; }
			set { SetPropertyValue(ref _scopeDECSlewMS, value); }
		}
		public string ScopeDECGuideMS
		{
			get { return _scopeDECGuideMS; }
			set { SetPropertyValue(ref _scopeDECGuideMS, value); }
		}

		/// <summary>
		/// Gets or sets the board of the scope 
		/// </summary>
		public string ScopeBoard
		{
			get { return _scopeBoard; }
			set { SetPropertyValue(ref _scopeBoard, value); }
		}

		/// <summary>
		/// Gets or sets the display of the scope 
		/// </summary>
		public string ScopeDisplay
		{
			get { return _scopeDisplay; }
			set { SetPropertyValue(ref _scopeDisplay, value); }
		}

		/// <summary>
		/// Gets or sets the features of the scope 
		/// </summary>
		public string ScopeFeatures
		{
			get { return _scopeFeatures; }
			set { SetPropertyValue(ref _scopeFeatures, value); }
		}

		public bool ScopeHasALT
		{
			get { return _scopeHasALT; }
			set { SetPropertyValue(ref _scopeHasALT, value); }
		}

		public bool ScopeHasAZ
		{
			get { return _scopeHasAZ; }
			set { SetPropertyValue(ref _scopeHasAZ, value); }
		}

		public bool ScopeHasFOC
		{
			get { return _scopeHasFOC; }
			set { SetPropertyValue(ref _scopeHasFOC, value); }
		}

		public bool ScopeHasHSAH
		{
			get { return _scopeHasHSAH; }
			set { SetPropertyValue(ref _scopeHasHSAH, value); }
		}

		public string ScopeLatitude
		{
			get { return _scopeLatitude; }
			set { SetPropertyValue(ref _scopeLatitude, value); }
		}

		public string ScopeLongitude
		{
			get { return _scopeLongitude; }
			set { SetPropertyValue(ref _scopeLongitude, value); }
		}

		public string ScopeTemperature
		{
			get { return _scopeTemperature; }
			set { SetPropertyValue(ref _scopeTemperature, value); }
		}

		public string ScopeDate
		{
			get { return _scopeDate; }
			set { SetPropertyValue(ref _scopeDate, value); }
		}

		public string ScopeTime
		{
			get { return _scopeTime; }
			set { SetPropertyValue(ref _scopeTime, value); }
		}

		public string ScopePolarisHourAngle
		{
			get { return _scopePolarisHourAngle; }
			set { SetPropertyValue(ref _scopePolarisHourAngle, value); }
		}

		public string ScopeSiderealTime
		{
			get { return _scopeSiderealTime; }
			set { SetPropertyValue(ref _scopeSiderealTime, value); }
		}

		public string ScopeNetworkState
		{
			get { return _scopeNetworkState; }
			set { SetPropertyValue(ref _scopeNetworkState, value); }
		}

		public string ScopeNetworkIPAddress
		{
			get { return _scopeNetworkIPAddress; }
			set { SetPropertyValue(ref _scopeNetworkIPAddress, value); }
		}

		public string ScopeNetworkSSID
		{
			get { return _scopeNetworkSSID; }
			set { SetPropertyValue(ref _scopeNetworkSSID, value); }
		}

		/// <summary>
		/// Gets or sets the status of the scope
		/// </summary>
		public string MountStatus
		{
			get { return _mountStatus; }
			set { SetPropertyValue(ref _mountStatus, value); }
		}

		/// <summary>
		/// Gets or sets the status of the scope
		/// </summary>
		public string DriftAlignStatus
		{
			get { return _driftAlignStatus; }
			set { SetPropertyValue(ref _driftAlignStatus, value); }
		}

		/// <summary>
		/// Gets or sets the name of the scope
		/// </summary>
		public string CurrentHA
		{
			get { return _currentHA; }
			set { SetPropertyValue(ref _currentHA, value); }
		}


		/// <summary>
		/// Gets or sets 
		/// </summary>
		public bool IsCoarseSlewing
		{
			get { return _isCoarseSlewing; }
			set { SetPropertyValue(ref _isCoarseSlewing, value, OnCoarseSlewingChanged); }
		}

		private void OnCoarseSlewingChanged(bool oldVal, bool newVal)
		{
			if (MountConnected)
			{
				this.SendOatCommand($":XSM{(newVal ? 0 : 1)}#", (a) => { });
			}
		}
		/// <summary>
		/// Gets or sets the status of the scope
		/// </summary>
		public bool ShowDECLimits
		{
			get { return _showDecLimits; }
			set { SetPropertyValue(ref _showDecLimits, value); }
		}

		/// <summary>
		/// Gets or sets the status of the scope
		/// </summary>
		public bool IsTracking
		{
			get { return _isTracking; }
			set { SetPropertyValue(ref _isTracking, value, OnTrackingChanged); }
		}

		private void OnTrackingChanged(bool oldVal, bool newVal)
		{
			if (MountConnected)
			{
				try
				{
					Task.Run(async () => await _oatMount.SetTracking(newVal));
				}
				catch (Exception ex)
				{
					Debug.WriteLine("Unable to set Tracking mode." + ex.Message);
				}
			}
		}

		/// <summary>
		/// Gets or sets the status of the scope
		/// </summary>
		public bool IsGuiding
		{
			get { return _isGuiding; }
			set { SetPropertyValue(ref _isGuiding, value); }
		}

		/// <summary>
		/// Gets or sets the status of the scope
		/// </summary>
		public bool IsSlewingNorth
		{
			get { return _isSlewingNorth; }
			set { SetPropertyValue(ref _isSlewingNorth, value); }
		}


		/// <summary>
		/// Gets or sets the status of the scope
		/// </summary>
		public bool IsSlewingSouth
		{
			get { return _isSlewingSouth; }
			set { SetPropertyValue(ref _isSlewingSouth, value); }
		}

		/// <summary>
		/// Gets or sets the status of the scope
		/// </summary>
		public bool IsSlewingEast
		{
			get { return _isSlewingEast; }
			set { SetPropertyValue(ref _isSlewingEast, value); }
		}

		/// <summary>
		/// Gets or sets the status of the scope
		/// </summary>
		public bool IsSlewingWest
		{
			get { return _isSlewingWest; }
			set { SetPropertyValue(ref _isSlewingWest, value); }
		}

		public string RemainingRATime
		{
			get { return _remainingRATime; }
			set { SetPropertyValue(ref _remainingRATime, value); }
		}

		/// <summary>
		/// </summary>
		public bool IsDriftAligning
		{
			get { return _driftAlignRunning; }
			set { SetPropertyValue(ref _driftAlignRunning, value); }
		}

		public float DriftPhase
		{
			get { return _driftPhase; }
			set { SetPropertyValue(ref _driftPhase, value); }
		}

		public float TrackingSpeed
		{
			get { return _trackingSpeed; }
			set { SetPropertyValue(ref _trackingSpeed, value, OnRAPosChangedFloat); }
		}

		private void OnRAPosChangedFloat(float arg1, float arg2)
		{
			OnRAPosChanged(0, 0);
		}

		public float MaxMotorSpeed
		{
			get { return _maxMotorSpeed; }
			set { SetPropertyValue(ref _maxMotorSpeed, value, SlewSpeedChanged); }
		}

		public int SlewRate
		{
			get { return _slewRate; }
			set { SetPropertyValue(ref _slewRate, value, SlewRateChanged); }
		}

		public float SlewXSpeed
		{
			get { return _slewXSpeed; }
			set { SetPropertyValue(ref _slewXSpeed, value, SlewSpeedChanged); }
		}

		public float SlewYSpeed
		{
			get { return _slewYSpeed; }
			set { SetPropertyValue(ref _slewYSpeed, value, SlewSpeedChanged); }
		}

		private void SlewRateChanged(int arg1, int newRate)
		{
			float[] speeds = { 0, 0.05f, 0.15f, 0.5f, 1.0f };
			string slewRateComdChar = "_GCMS";

			MaxMotorSpeed = speeds[newRate] * 2.5f; // Can't go much quicker than 2.5 degs/sec

			if (MountConnected)
			{
				var slewChange = $":R{slewRateComdChar[newRate]}#";
				this.SendOatCommand(slewChange, (a) => { });
			}
		}

		private void SlewSpeedChanged(float arg1, float arg2)
		{
			UpdateMotorSpeeds(-_slewXSpeed * MaxMotorSpeed, -_slewYSpeed * MaxMotorSpeed * 1.5f);
		}

		private void UpdateMotorSpeeds(float v1, float v2)
		{
			lock (_speedUpdateLock)
			{
				_updatedSpeeds = (RASpeed != v1) || (DECSpeed != v2);
				RASpeed = v1;
				DECSpeed = v2;
			}
		}

		private async Task OnFineSlewTimer(object s, EventArgs e)
		{
			_timerFineSlew.Stop();
			if (MountConnected)
			{
				if (!_isCoarseSlewing)
				{
					double raSpeed;
					double decSpeed;
					bool doUpdate = false;
					lock (_speedUpdateLock)
					{
						raSpeed = RASpeed;
						decSpeed = DECSpeed;
						doUpdate = _updatedSpeeds;
						_updatedSpeeds = false;
					}

					if (doUpdate)
					{
						var doneEvent = new AsyncAutoResetEvent();
						var ras = string.Format(_oatCulture, ":XSX{0:0.000000}#", raSpeed);
						var decs = string.Format(_oatCulture, ":XSY{0:0.000000}#", decSpeed);
						this.SendOatCommand(ras, (raResult) => { });
						this.SendOatCommand(decs, (decResult) => { doneEvent.Set(); });
						await doneEvent.WaitAsync();
					}
				}
			}
			_timerFineSlew.Start();
		}

		public long FirmwareVersion
		{
			get { return _firmwareVersion; }
			set { SetPropertyValue(ref _firmwareVersion, value); }
		}

		public PointOfInterest SelectedPointOfInterest
		{
			get { return _selectedPointOfInterest; }
			set { SetPropertyValue(ref _selectedPointOfInterest, value, (oldV, newV) => SetTargetFromPOI(newV)); }
		}

		private void SetTargetFromPOI(PointOfInterest poi)
		{
			int h, m, s;
			if (poi != null)
			{
				FloatToHMS(poi.DEC, out h, out m, out s);
				_targetDEC.SetTime(h, m, s);

				FloatToHMS(poi.RA, out h, out m, out s);
				_targetRA.SetTime(h, m, s);
				OnTargetChanged(0, 0);
			}
		}

		public List<PointOfInterest> AvailablePointsOfInterest
		{
			get { return _pointsOfInterest; }
		}

		/// <summary>
		/// Gets the string for the connect button
		/// </summary>
		public string ConnectCommandString
		{
			get { return MountConnected ? "Disconnect" : "Connect"; }
		}

		public string ParkCommandString
		{
			get { return _parkString; }
			set { SetPropertyValue(ref _parkString, value); }
		}

		public Version Version { get; private set; }

		internal void SetDecLowLimit()
		{
			DECStepperLowerLimit = DECStepper / DECStepsPerDegree;
		}

		internal void TargetChooserClosed()
		{
			_targetChooser = null;
		}

		public String SelectedBaudRate
		{
			get { return _serialBaudRate; }
			set
			{
				_serialBaudRate = value;
				AppSettings.Instance.BaudRate = value;
			}
		}

		public IEnumerable<String> AvailableBaudRates
		{
			get { return _baudRates; }
		}

		List<String> _baudRates = new List<string>() {
					"230400",
					"115200",
					"57600",
					"38400",
					"28800",
					"19200",
					"14400",
					"9600",
					"4800",
					"2400",
					"1200",
					"300",
				};

		/// <summary>
		/// Gets or sets the keep mini control on-top
		/// </summary>
		public bool KeepMiniControlOnTop
		{
			get { return _keepMiniControllerOnTop; }
			set
			{
				SetPropertyValue(ref _keepMiniControllerOnTop, value);
				if (_miniController != null)
				{
					_miniController.Topmost = value;
				}
			}
		}
		private void OnSimulationClientConnect()
		{
			if (_oatSimComm != null && _oatSimComm.IsClientConnected == true)
			{
				_oatSimComm.Send($"Version|{Version}");
				_oatSimComm.Send($"FirmwareVersion|{ScopeVersion}");
				_oatSimComm.Send($"ScopeRASlewMS|{ScopeRASlewMS}");
				_oatSimComm.Send($"ScopeRATrackMS|{ScopeRATrackMS}");
				_oatSimComm.Send($"ScopeDECSlewMS|{ScopeDECSlewMS}");
				_oatSimComm.Send($"ScopeDECGuideMS|{ScopeDECGuideMS}");
				_oatSimComm.Send($"ScopeLongitude|{ScopeLongitude}");
				_oatSimComm.Send($"ScopeLatitude|{ScopeLatitude}");
				_oatSimComm.Send($"RAStepsPerDegree|{RAStepsPerDegree}");
				_oatSimComm.Send($"DECStepsPerDegree|{DECStepsPerDegree}");
			}
		}

		private void UpdateSimulationClient()
		{
			if (_oatSimComm != null && _oatSimComm.IsClientConnected == true)
			{
				_oatSimComm.Send($"CurrentRAString|{CurrentRAHour},{CurrentRAMinute},{CurrentRASecond}");
				_oatSimComm.Send($"CurrentDECString|{CurrentDECSign}{CurrentDECDegree},{CurrentDECMinute},{CurrentDECSecond}");
				_oatSimComm.Send($"RAStepper|{RAStepper}");
				_oatSimComm.Send($"DECStepper|{DECStepper}");
				_oatSimComm.Send($"TrkStepper|{TrkStepper}");
				_oatSimComm.Send($"ScopeSiderealTime|{ScopeSiderealTime}");
				_oatSimComm.Send($"ScopePolarisHourAngle|{ScopePolarisHourAngle}");
				_oatSimComm.Send($"ScopeTime|{ScopeTime}");
			}
		}

		private void OnSimationClientCommand(string cmd)
		{
			this.SendOatCommand(cmd, (a) => { });
		}
	}
}

