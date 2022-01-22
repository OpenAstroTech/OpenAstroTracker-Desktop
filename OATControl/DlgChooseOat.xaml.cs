using MahApps.Metro.Controls;
using OATCommuncations.WPF;
using OATCommunications;
using OATCommunications.Model;
using OATCommunications.WPF.CommunicationHandlers;
using OATControl.Properties;
using OATControl.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using CommandResponse = OATCommunications.CommunicationHandlers.CommandResponse;

namespace OATControl
{
	/// <summary>
	/// Interaction logic for DlgChooseOat.xaml
	/// </summary>
	public partial class DlgChooseOat : MetroWindow, INotifyPropertyChanged
	{
		public class DeviceDriver
		{
			public DeviceDriver(string name, bool hasSetup, ICommand runSetup)
			{
				DeviceName = name;
				SupportsSetup = hasSetup;
				RunSetupCommand = runSetup;
			}

			public string DeviceName { get; set; }
			public bool SupportsSetup { get; set; }
			public ICommand RunSetupCommand { get; set; }
		}

		public enum Steps
		{
			Idle,
			WaitForDeviceConfirm,
			WaitForBaudrate,
			WaitForConnection,
			CheckHardware,
			WaitForLevel,
			WaitForGPS,
			ConfirmLocation,
			ConfirmStartupActions,
			Completed
		};

		private DelegateCommand _rescanCommand;
		private DelegateCommand _connectAndNextCommand;
		private float _latitude = 15;
		private float _longitude = -15;
		private float _altitude = 100;
		private DeviceDriver _device;
		private bool _showGPSStatus = false;
		private bool _showManualLocation = false;
		private bool _showBaudRate = false;
		private bool _showNextButton = false;
		private bool _showLevelDisplay = false;
		private bool _showRAHoming = false;
		private bool _showDECHoming = false;
		private bool _runRAAutoHoming = false;
		private bool _runDECOffsetHoming = false;
		private double _rollOffset;
		private double _pitchOffset;
		private List<double> _rollOffsetHistory;
		private List<double> _pitchOffsetHistory;
		private float _rollReference;
		private float _pitchReference;
		private MountVM _mountViewModel;
		private Steps _currentStep = Steps.Idle;
		private string _gpsStatus = string.Empty;
		private Action<string, Action<CommandResponse>> _sendCommand;
		private DispatcherTimer stateTimer;
		const float MaxWaitForGPS = 30;
		DateTime _startedGPSWaitAt;
		CultureInfo _oatCulture = new CultureInfo("en-US");
		public event PropertyChangedEventHandler PropertyChanged;

		public DlgChooseOat(MountVM mountViewModel, Action<string, Action<CommandResponse>> sendCommand)
		{
			AvailableDevices = new ObservableCollection<DeviceDriver>();
			stateTimer = new DispatcherTimer();
			stateTimer.Tick += new EventHandler(ProcessStateMachine);
			stateTimer.Interval = TimeSpan.FromMilliseconds(100);

			_sendCommand = sendCommand;
			_mountViewModel = mountViewModel;
			_rollOffsetHistory = new List<double>(5);
			_pitchOffsetHistory = new List<double>(5);
			_latitude = AppSettings.Instance.SiteLatitude;
			_longitude = AppSettings.Instance.SiteLongitude;
			_altitude = AppSettings.Instance.SiteAltitude;
			_runRAAutoHoming = AppSettings.Instance.RunAutoHomeRAOnConnect;
			_runDECOffsetHoming = AppSettings.Instance.RunDECOffsetHomingOnConnect;

			CurrentStep = Steps.Idle;
			_rescanCommand = new DelegateCommand(async () => { await OnDiscoverDevices(); }, () => (_currentStep == Steps.Idle) || (_currentStep == Steps.WaitForBaudrate));
			_connectAndNextCommand = new DelegateCommand((o) => AdvanceStateMachine(), () => IsNextEnabled);

			this.DataContext = this;
			InitializeComponent();

			this.Result = false;

			Task.Run(async () => { await OnDiscoverDevices(); });

			stateTimer.Start();
		}

		public async Task OnDiscoverDevices()
		{
			WpfUtilities.RunOnUiThread(() => { AvailableDevices.Clear(); }, Application.Current.Dispatcher);

			CommunicationHandlerFactory.DiscoverDevices();
			await Task.Delay(500);

			foreach (var device in CommunicationHandlerFactory.AvailableDevices)
			{
				var handler = CommunicationHandlerFactory.AvailableHandlers.First(h => h.IsDriverForDevice(device));
				var driver = new DeviceDriver(device, handler.SupportsSetupDialog, new DelegateCommand((p) => OnRunDeviceHandlerSetup(handler, p)));
				WpfUtilities.RunOnUiThread(() => { AvailableDevices.Add(driver); }, Application.Current.Dispatcher);
			}
		}

		public ICommand RescanCommand { get { return _rescanCommand; } }
		public ICommand ConnectAndNextCommand { get { return _connectAndNextCommand; } }

		public ObservableCollection<DeviceDriver> AvailableDevices { get; private set; }

		private void OnRunDeviceHandlerSetup(ICommunicationHandler handler, object device)
		{
			handler.RunSetupDialog();
		}

		public DeviceDriver SelectedDevice
		{
			get { return _device; }
			set
			{
				if (value != _device)
				{
					_device = value;
					if (CurrentStep == Steps.Idle)
					{
						AdvanceStateMachine();
					}
					OnPropertyChanged("SelectedDevice");
				}
			}
		}

		public string GPSStatus
		{
			get { return _gpsStatus; }
			set
			{
				if (value != _gpsStatus)
				{
					_gpsStatus = value;
					OnPropertyChanged("GPSStatus");
				}
			}
		}

		public bool ShowNextButton
		{
			get { return _showNextButton; }
			set
			{
				if (value != _showNextButton)
				{
					_showNextButton = value;
					OnPropertyChanged("ShowNextButton");
				}
			}
		}

		public bool ShowBaudRate
		{
			get { return _showBaudRate; }
			set
			{
				if (value != _showBaudRate)
				{
					_showBaudRate = value;
					OnPropertyChanged("ShowBaudRate");
				}
			}
		}

		public bool ShowManualLocation
		{
			get { return _showManualLocation; }
			set
			{
				if (value != _showManualLocation)
				{
					_showManualLocation = value;
					OnPropertyChanged("ShowManualLocation");
				}
			}
		}

		public bool ShowGPSStatus
		{
			get { return _showGPSStatus; }
			set
			{
				if (value != _showGPSStatus)
				{
					_showGPSStatus = value;
					OnPropertyChanged("ShowGPSStatus");
				}
			}
		}

		public bool ShowLevelDisplay
		{
			get { return _showLevelDisplay; }
			set
			{
				if (value != _showLevelDisplay)
				{
					_showLevelDisplay = value;
					OnPropertyChanged("ShowLevelDisplay");
				}
			}
		}

		public bool RunRAAutoHoming
		{
			get { return _runRAAutoHoming; }
			set
			{
				if (value != _runRAAutoHoming)
				{
					_runRAAutoHoming = value;
					OnPropertyChanged("RunRAAutoHoming");
				}
			}
		}

		public bool RunDECOffsetHoming
		{
			get { return _runDECOffsetHoming; }
			set
			{
				if (value != _runDECOffsetHoming)
				{
					_runDECOffsetHoming = value;
					OnPropertyChanged("RunDECOffsetHoming");
				}
			}
		}

		public double RollOffset
		{
			get { return _rollOffset; }
			set
			{
				if (value != _rollOffset)
				{
					_rollOffset = value;
					OnPropertyChanged("RollOffset");
				}
			}
		}

		public double PitchOffset
		{
			get { return _pitchOffset; }
			set
			{
				if (value != _pitchOffset)
				{
					_pitchOffset = value;
					OnPropertyChanged("PitchOffset");
				}
			}
		}

		public bool ShowRAHoming
		{
			get
			{
				return _showRAHoming;
			}
			set
			{
				if (value != _showRAHoming)
				{
					_showRAHoming = value;
					OnPropertyChanged("ShowRAHoming");
				}
			}
		}

		public bool ShowDECHoming
		{
			get
			{
				return _showDECHoming;
			}
			set
			{
				if (value != _showDECHoming)
				{
					_showDECHoming = value;
					OnPropertyChanged("ShowDECHoming");
				}
			}
		}

		public Steps CurrentStep
		{
			get { return _currentStep; }
			set
			{
				if (value != _currentStep)
				{
					_currentStep = value;
					OnPropertyChanged("CurrentStep");
				}
			}
		}

		public float Latitude
		{
			get { return _latitude; }
			set
			{
				if (value != _latitude)
				{
					_latitude = value;
					OnPropertyChanged("Latitude");
				}
			}
		}

		public float Longitude
		{
			get { return _longitude; }
			set
			{
				if (value != _longitude)
				{
					_longitude = value;
					OnPropertyChanged("Longitude");
				}
			}
		}

		public float Altitude
		{
			get { return _altitude; }
			set
			{
				if (value != _altitude)
				{
					_altitude = value;
					OnPropertyChanged("Altitude");
				}
			}
		}

		private void OnPropertyChanged(string field)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(field));
				RequeryCommands();
			}
		}
		private void RequeryCommands()
		{
			_rescanCommand.Requery();
			_connectAndNextCommand.Requery();
		}

		protected override void OnClosed(EventArgs e)
		{
			stateTimer.Stop();
			base.OnClosed(e);
		}

		protected bool TryParseLatLong(string latlong, ref float fLatLong)
		{
			var parts = latlong.Split('*', '\'');
			fLatLong = 1.0f * int.Parse(parts[0]) + int.Parse(parts[1]) / 60.0f;
			return true;
		}

		public bool? Result { get; set; }

		private void AdvanceStateMachine()
		{
			switch (CurrentStep)
			{
				case Steps.Idle: // Clicked on device
					CurrentStep = Steps.WaitForDeviceConfirm;
					ShowNextButton = true;
					break;

				case Steps.WaitForDeviceConfirm: // Clicked on Next
					if (SelectedDevice.DeviceName.StartsWith("Serial"))
					{
						CurrentStep = Steps.WaitForBaudrate;
						ShowBaudRate = true;
					}
					else
					{
						CurrentStep = Steps.CheckHardware;
						GPSStatus = $"Connecting to OAT on {SelectedDevice.DeviceName}";
						ShowGPSStatus = true;
					}

					ShowNextButton = !string.IsNullOrEmpty(SelectedBaudRate);
					break;

				case Steps.WaitForBaudrate:
				case Steps.WaitForConnection:
					GPSStatus = $"Connecting to OAT on {SelectedDevice.DeviceName}{(SelectedDevice.DeviceName.StartsWith("Serial") ? " at " + SelectedBaudRate + " baud" : "")}";
					ShowGPSStatus = true;
					CurrentStep = Steps.CheckHardware;
					break;

				case Steps.CheckHardware:
					// NOP
					break;

				case Steps.WaitForLevel:
					// Turn off the Level
					var doneEvent = new AutoResetEvent(false);
					_sendCommand(":XL0#,#", (a) => { doneEvent.Set(); });
					doneEvent.WaitOne();

					if (_mountViewModel.IsAddonSupported("GPS"))
					{
						CurrentStep = Steps.WaitForGPS;
						ShowGPSStatus = true;
						_startedGPSWaitAt = DateTime.UtcNow;
					}
					else
					{
						ShowManualLocation = true;
						CurrentStep = Steps.ConfirmLocation;
					}
					break;

				case Steps.WaitForGPS:
					// We have a GPS, but the user is too impatient. We'll assume the location that OAT has stored is accurate and use that.
					ShowManualLocation = true;
					CurrentStep = Steps.ConfirmLocation;
					GPSStatus = "GPS acquisition cancelled, please enter location:";
					var locDoneEvent = new AutoResetEvent(false);
					bool gotLoc = false;
					float lat = 0, lng = 0;
					_sendCommand(":Gt#,#", (a) => { gotLoc = a.Success && TryParseLatLong(a.Data, ref lat); });
					_sendCommand(":Gg#,#", (a) => { gotLoc = gotLoc && a.Success && TryParseLatLong(a.Data, ref lng); locDoneEvent.Set(); });
					locDoneEvent.WaitOne();
					if (gotLoc)
					{
						Latitude = lat;
						Longitude = 180.0f - lng;
					}
					break;

				case Steps.ConfirmLocation:
					GPSStatus = "Sync'd! Setting OAT location...";
					Task.Run(() => _mountViewModel.SetSiteLatitude(Latitude)).Wait();
					Task.Run(() => _mountViewModel.SetSiteLongitude(Longitude)).Wait();

					AppSettings.Instance.SiteLatitude = Latitude;
					AppSettings.Instance.SiteLongitude = Longitude;
					AppSettings.Instance.SiteAltitude = Altitude;
					AppSettings.Instance.RunAutoHomeRAOnConnect = RunRAAutoHoming;
					AppSettings.Instance.RunDECOffsetHomingOnConnect = RunDECOffsetHoming;
					AppSettings.Instance.Save();
					CurrentStep = Steps.Completed;
					break;

				case Steps.ConfirmStartupActions:
					AppSettings.Instance.RunAutoHomeRAOnConnect = RunRAAutoHoming;
					AppSettings.Instance.RunDECOffsetHomingOnConnect = RunDECOffsetHoming;
					AppSettings.Instance.Save();
					CurrentStep = Steps.Completed;
					break;

				case Steps.Completed:
					this.Result = true;
					this.Close();
					break;

				default: break;
			}
		}

		private async void ProcessStateMachine(object sender, EventArgs e)
		{
			stateTimer.Stop();
			switch (CurrentStep)
			{
				case Steps.WaitForBaudrate:
					break;

				case Steps.WaitForConnection:
					break;

				case Steps.CheckHardware:
				{
					var connectResult = await _mountViewModel.ConnectToOat(SelectedDevice.DeviceName + "@" + SelectedBaudRate);

					ShowGPSStatus = false;
					GPSStatus = string.Empty;

					if (!connectResult.Item1)
					{
						ShowGPSStatus = true;
						GPSStatus = connectResult.Item2;
						CurrentStep = Steps.WaitForConnection;
						_mountViewModel.ConnectionState = string.Empty;
						return;
					}

					if (_mountViewModel.IsAddonSupported("GYRO"))
					{
						CurrentStep = Steps.WaitForLevel;

						// Turn on the Digital Level
						var doneEvent = new AutoResetEvent(false);
						_sendCommand(":XL1#,#", (a) => { });
						Thread.Sleep(100);
						// Get the reference angles from the level.
						_sendCommand(":XLGR#,#", (a) =>
						{
							if (a.Success)
							{
								string referenceAngles = a.Data;
								var angles = referenceAngles.Split(",".ToCharArray());
								float pitch, roll;
								float.TryParse(angles[0], NumberStyles.Float, _oatCulture, out _pitchReference);
								float.TryParse(angles[1], NumberStyles.Float, _oatCulture, out _rollReference);
							}
							doneEvent.Set();
						});
						doneEvent.WaitOne();
						ShowLevelDisplay = true;
					}
					else if (_mountViewModel.IsAddonSupported("GPS"))
					{
						CurrentStep = Steps.WaitForGPS;
						ShowGPSStatus = true;
						_startedGPSWaitAt = DateTime.UtcNow;
					}
					else
					{
						ShowManualLocation = true;
						CurrentStep = Steps.ConfirmLocation;
						// Let's get the coordinate stored on the OAT
						var locDoneEvent = new AutoResetEvent(false);
						bool gotLoc = false;
						float lat = 0, lng = 0;
						_sendCommand(":Gt#,#", (a) => { gotLoc = a.Success && TryParseLatLong(a.Data, ref lat); });
						_sendCommand(":Gg#,#", (a) => { gotLoc = gotLoc && a.Success && TryParseLatLong(a.Data, ref lng); locDoneEvent.Set(); });
						locDoneEvent.WaitOne();
						if (gotLoc)
						{
							Latitude = lat;
							Longitude = 180.0f - lng;
						}
						if ((_mountViewModel.ScopeHasHSAH) || (_mountViewModel.DECStepperLowerLimit != 0))
						{
							ShowRAHoming = _mountViewModel.ScopeHasHSAH;
							ShowDECHoming = _mountViewModel.DECStepperLowerLimit != 0;

							var gxDoneEvent = new AutoResetEvent(false);
							var gotGX = false;
							string gxValue = string.Empty;
							_sendCommand(":GX#,#", (a) => { gotGX = a.Success; gxValue = a.Data; gxDoneEvent.Set(); });
							gxDoneEvent.WaitOne();
							if (gotGX)
							{
								// 'SlewToTarget,-dT---,0,13919,1861,034107,+454030,50000,'
								var gxSplit = gxValue.Split(",".ToCharArray());
								if (long.Parse(gxSplit[3]) != 0) { RunDECOffsetHoming = false; }
							}
						}
						break;
					}
				}
				break;

				case Steps.WaitForLevel:
				{
					var doneEvent = new AsyncAutoResetEvent();
					// Get the current digital level angles.
					_sendCommand(":XLGC#,#", (a) =>
					{
						string currentAngles = a.Data;
						if (a.Success && !currentAngles.Contains("NAN"))
						{
							var angles = currentAngles.Split(",".ToCharArray());
							float.TryParse(angles[0], NumberStyles.Float, _oatCulture, out float currentPitch);
							float.TryParse(angles[1], NumberStyles.Float, _oatCulture, out float currentRoll);

							// Keep a rolling average of the last 3 values.
							if (_rollOffsetHistory.Count > 2)
							{
								_rollOffsetHistory.RemoveAt(0);
							}
							if (_pitchOffsetHistory.Count > 2)
							{
								_pitchOffsetHistory.RemoveAt(0);
							}

							_rollOffsetHistory.Add(currentRoll - _rollReference);
							_pitchOffsetHistory.Add(currentPitch - _pitchReference);
						}
						doneEvent.Set();
					});
					await doneEvent.WaitAsync();
					if (_rollOffsetHistory.Any())
					{
						RollOffset = _rollOffsetHistory.Average();
					}
					if (_pitchOffsetHistory.Any())
					{
						PitchOffset = _pitchOffsetHistory.Average();
					}
				}
				break;

				case Steps.WaitForGPS:
				{
					TimeSpan elapsed;
					elapsed = DateTime.UtcNow - _startedGPSWaitAt;
					if (elapsed.TotalSeconds >= MaxWaitForGPS)
					{
						GPSStatus = "GPS could not get a location lock. Please enter location manually:";
						ShowManualLocation = true;
						CurrentStep = Steps.ConfirmLocation;
						break;
					}

					GPSStatus = $"Waiting {MaxWaitForGPS - elapsed.TotalSeconds:0}s for GPS to find satellites and sync...";
					await Task.Delay(150);

					bool gpsIsSyncd = false;
					var doneEvent = new AutoResetEvent(false);
					_sendCommand(":gT100#,n", async (result) =>
					{
						if (result.Data == "1")
						{
							gpsIsSyncd = true;
						}
						else
						{
							await Task.Delay(500);
						}
						doneEvent.Set();
					});
					doneEvent.WaitOne(2500);

					if (gpsIsSyncd)
					{
						GPSStatus = "Sync'd! Retrieving current location...";
						float latitude = 0;
						float longitude = 0;
						bool gotLoc = false;
						var doneEventLatLong = new AutoResetEvent(false);
						_sendCommand(":Gt#,#", (a) => { gotLoc = a.Success && TryParseLatLong(a.Data, ref latitude); });
						_sendCommand(":Gg#,#", (a) => { gotLoc = gotLoc && a.Success && TryParseLatLong(a.Data, ref longitude); doneEventLatLong.Set(); });
						doneEventLatLong.WaitOne();
						if (gotLoc)
						{
							longitude = 180 - longitude;

							GPSStatus = "Sync'd! Setting OAT location...";
							await _mountViewModel.SetSiteLatitude(latitude);
							await _mountViewModel.SetSiteLongitude(longitude);
							await Task.Delay(400);
						}

						AppSettings.Instance.SiteLatitude = latitude;
						AppSettings.Instance.SiteLongitude = longitude;
						AppSettings.Instance.Save();
						if ((_mountViewModel.ScopeHasHSAH) || (_mountViewModel.DECStepperLowerLimit != 0))
						{
							ShowRAHoming = _mountViewModel.ScopeHasHSAH;
							ShowDECHoming = _mountViewModel.DECStepperLowerLimit != 0;
							CurrentStep = Steps.ConfirmStartupActions;
						}
						else
						{
							CurrentStep = Steps.Completed;
						}
					}
				}
				break;

				case Steps.ConfirmLocation:
				case Steps.ConfirmStartupActions:
					break;

				case Steps.Completed:
					this.Result = true;
					this.Close();
					return;

				default: break;
			}

			stateTimer.Start();
		}

		public bool IsNextEnabled
		{
			get
			{
				switch (CurrentStep)
				{
					case Steps.Idle:
						return false;

					case Steps.WaitForBaudrate:
						return !string.IsNullOrEmpty(SelectedBaudRate);

					case Steps.WaitForDeviceConfirm:
					case Steps.WaitForConnection:
						return SelectedDevice != null;

					case Steps.CheckHardware:
						return false;

					case Steps.WaitForLevel:
						return true;

					case Steps.WaitForGPS:
						return true;

					case Steps.ConfirmLocation:
						return true;

					case Steps.ConfirmStartupActions:
						return true;

					case Steps.Completed:
						return true;

					default: break;
				}
				return false;
			}
		}

		private void Device_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (SelectedDevice != null)
			{
				CurrentStep = SelectedDevice.DeviceName.StartsWith("Serial") ? Steps.WaitForDeviceConfirm : Steps.WaitForConnection;
				AdvanceStateMachine();
			}
		}

		public String SelectedBaudRate
		{
			get { return _mountViewModel.SelectedBaudRate; }
			set { _mountViewModel.SelectedBaudRate = value; }
		}

		public IEnumerable<String> AvailableBaudRates
		{
			get { return _mountViewModel.AvailableBaudRates; }
		}

	}
}