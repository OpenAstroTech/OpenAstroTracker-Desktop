using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using OATCommunications;
using OATCommunications.Model;
using OATCommunications.WPF;
using OATCommunications.WPF.CommunicationHandlers;

namespace OATTest
{
	public class TestVM : NotifyPropertyChanged
	{
		TestManager _testManager;
		ObservableCollection<string> _debugOutput;
		ICommunicationHandler _handler;
		AsyncAutoResetEvent _asyncAutoResetEvent = new AsyncAutoResetEvent();
		CultureInfo _oatCulture = new CultureInfo("en-US");
		string _commandPort;
		string _debugPort;
		bool _seperateDebugPort;
		DateTime _useDateTime;
		DateTime _presetTime;
		DelegateCommand _setDateTimePresetCommand;
		DelegateCommand _setDateTimeCommand;
		DelegateCommand _runTestsCommand;
		DelegateCommand _resetTestsCommand;
		private bool _failed;
		private string _debugBaudRate;
		private string _commandBaudRate;
		private List<string> _baudRates;
		private string _appStatus;
		private string _runButtonText = "Run Test Suite";

		public ICommand SetDateTimeToPresetCommand { get { return _setDateTimePresetCommand; } }
		public ICommand SetDateTimeToNowCommand { get { return _setDateTimeCommand; } }
		public ICommand RunTestsCommand { get { return _runTestsCommand; } }
		public ICommand ResetTestsCommand { get { return _resetTestsCommand; } }

		public TestVM()
		{
			_useDateTime = DateTime.Parse("03/28/22 23:00:00");
			_presetTime = _useDateTime;
			CommunicationHandlerFactory.Initialize();
			_testManager = new TestManager();
			_seperateDebugPort = false;
			_testManager.LoadTestSuite(@"c:\Users\Lutz.KRETZSCHMAR.000\Source\OpenAstroTracker-Desktop\OATTest\TestSuite.xml");
			_debugOutput = new ObservableCollection<string>();
			OnPropertyChanged("Tests");
			_debugOutput.Add("Welcome to TestManager V1.0");
			_debugOutput.Add("Test suite loaded");
			_setDateTimeCommand = new DelegateCommand(s => OnSetDateTime(true));
			_setDateTimePresetCommand = new DelegateCommand(s => OnSetDateTime(false));
			_runTestsCommand = new DelegateCommand(async (s) => await OnStartTests());
			_resetTestsCommand = new DelegateCommand(() => OnResetTests());
			_commandBaudRate = "19200";
			_debugBaudRate = "57600";

			AvailableDevices = new ObservableCollection<string>();
			AvailableBaudRates = new List<string>() { "9600", "19200", "28800", "38400", "57600", "115200" };

			CommunicationHandlerFactory.DiscoverDevices();

			Task.Run(async () => { await OnDiscoverDevices(); });
		}

		void Debug(string line)
		{
			WpfUtilities.RunOnUiThread(() => _debugOutput.Add(line), Application.Current.Dispatcher);
		}

		public void OnResetTests()
		{
			_testManager.ResetAllTests();
			AppStatus = string.Empty;
		}

		private async Task OnStartTests()
		{
			if (_testManager.AreTestsRunning)
			{
				AppStatus = "Aborting tests...";
				RunButtonText = "Waiting...";
				_testManager.AbortRun();
				_asyncAutoResetEvent.Set();
				return;
			}

			RunButtonText = "Abort";
			AppStatus = "Preparing tests...";
			_testManager.UseDate = _useDateTime;
			_testManager.ResetAllTests();
			_testManager.PrepareForRun();

			_failed = true;
			try
			{
				string port = CommandPort + "@" + _commandBaudRate;
				Debug($"Connecting to OAT on {port}...");
				AppStatus = "Connecting to OAT ...";
				_handler = CommunicationHandlerFactory.ConnectToDevice(port);
				_handler.Connect();
				await Task.Delay(3000);
				_handler.SendCommand(":GVN#", (resultNr) =>
				{
					if (resultNr.Success)
					{
						var versionNumbers = resultNr.Data.Substring(1).Split(".".ToCharArray());
						if (versionNumbers.Length != 3)
						{
							Debug($"Unrecognizable firmware version '{resultNr.Data}'");
						}
						else
						{
							try
							{
								FirmwareVersion = long.Parse(versionNumbers[0]) * 10000L + long.Parse(versionNumbers[1]) * 100L + long.Parse(versionNumbers[2]);
								_testManager.FirmwareVersion = FirmwareVersion;

								AppStatus = $"Connected to OAT {resultNr.Data}, running tests...";

								Debug($"OAT is running firmware version '{resultNr.Data}' => {FirmwareVersion}");
								_failed = false;
							}
							catch
							{
								Debug($"Unable to parse firmware version '{resultNr.Data}'");
							}
						}
					}
					_asyncAutoResetEvent.Set();
				});

				await _asyncAutoResetEvent.WaitAsync();
			}
			catch (Exception ex)
			{
				Debug("Failed to connect. " + ex.Message);
			}

			if (!_failed)
			{
				Debug($"Connected to OAT with V{FirmwareVersion}, running tests...");
				_testManager.PrepareForRun();
				await _testManager.RunAllTests(_handler, (s) => Debug(s));

				Debug($"Tests complete, disconnecting...");
			}
			else
			{
				Debug($"Failed to connect to OAT, no tests run.");
			}

			AppStatus = $"Disconnecting...";

			if (_handler.Connected)
			{
				_handler.Disconnect();
			}

			Debug($"Finished.");
			long failed = _testManager.Tests.Sum(t => t.Status == CommandTest.StatusType.Failed ? 1 : 0);
			long succeeded = _testManager.Tests.Sum(t => t.Status == CommandTest.StatusType.Success ? 1 : 0);
			long skipped = _testManager.Tests.Sum(t => t.Status == CommandTest.StatusType.Skipped ? 1 : 0);

			AppStatus = $"{_testManager.Tests.Count} Tests completed. {failed} failed, {succeeded} succeeded. {skipped} skipped.";

			RunButtonText = "Run Test Suite";
		}

		private void OnSetDateTime(bool useNow)
		{
			_useDateTime = useNow ? DateTime.Now : _presetTime;
			OnPropertyChanged("UseTime");
			OnPropertyChanged("UseDate");
		}

		public ObservableCollection<string> AvailableDevices { get; private set; }
		public List<string> AvailableBaudRates { get { return _baudRates; } set { _baudRates = value; } }

		public bool CanRun
		{
			get
			{
				return !string.IsNullOrEmpty(CommandPort) && !string.IsNullOrEmpty(_commandBaudRate);
			}
		}

		public bool CanReset
		{
			get
			{
				return true;
			}
		}

		public async Task OnDiscoverDevices()
		{
			WpfUtilities.RunOnUiThread(() => { AvailableDevices.Clear(); }, Application.Current.Dispatcher);

			CommunicationHandlerFactory.DiscoverDevices();
			await Task.Delay(500);

			foreach (var device in CommunicationHandlerFactory.AvailableDevices)
			{
				//var handler = CommunicationHandlerFactory.AvailableHandlers.First(h => h.IsDriverForDevice(device));
				//var driver = new DeviceDriver(device, handler.SupportsSetupDialog, new DelegateCommand((p) => OnRunDeviceHandlerSetup(handler, p)));
				WpfUtilities.RunOnUiThread(() => { AvailableDevices.Add(device); }, Application.Current.Dispatcher);
			}
		}

		public IList<CommandTest> Tests
		{
			get { return _testManager.Tests; }
		}

		public ObservableCollection<string> DebugOutput
		{
			get
			{
				return _debugOutput;
			}
		}

		public string CommandPort
		{
			get { return _commandPort; }
			set
			{
				if (_commandPort != value)
				{
					_commandPort = value;
					if (!_seperateDebugPort)
					{
						DebugPort = value;
					}
					OnPropertyChanged();
					OnPropertyChanged("CanRun");
				}
			}
		}

		public string DebugPort
		{
			get { return _debugPort; }
			set
			{
				if (_debugPort != value)
				{
					_debugPort = value;
					OnPropertyChanged();
				}
			}
		}

		public bool IsDebugPortSeperate
		{
			get { return _seperateDebugPort; }
			set
			{
				if (_seperateDebugPort != value)
				{
					_seperateDebugPort = value;
					DebugPort = CommandPort;
					OnPropertyChanged();
				}
			}
		}

		public string UseDate
		{
			get { return _useDateTime.ToString("MM/dd/yy"); }
			set
			{
				DateTime date;
				if (DateTime.TryParse(value, out date))
				{
					_useDateTime = new DateTime(date.Year, date.Month, date.Day, _useDateTime.Hour, _useDateTime.Minute, _useDateTime.Second);
					OnPropertyChanged();
				}
			}
		}

		public string UseTime
		{
			get { return _useDateTime.ToString("HH:mm:ss"); }
			set
			{
				DateTime time;
				if (DateTime.TryParse(value, out time))
				{
					_useDateTime = new DateTime(_useDateTime.Year, _useDateTime.Month, _useDateTime.Day, time.Hour, time.Minute, time.Second);
					OnPropertyChanged();
				}
			}
		}
		
		public string RunButtonText
		{
			get { return _runButtonText; }
			set
			{
				if (_runButtonText != value)
				{
					_runButtonText = value;
					OnPropertyChanged();
				}
			}
		}
		public string DebugBaudRate
		{
			get { return _debugBaudRate; }
			set
			{
				if (_debugBaudRate != value)
				{
					_debugBaudRate = value;
					OnPropertyChanged();
				}
			}
		}

		public string CommandBaudRate
		{
			get { return _commandBaudRate; }
			set
			{
				if (_commandBaudRate != value)
				{
					_commandBaudRate = value;
					OnPropertyChanged();
					OnPropertyChanged("CanRun");
				}
			}
		}

		public string AppStatus
		{
			get { return _appStatus; }
			set
			{
				if (_appStatus != value)
				{
					_appStatus = value;
					OnPropertyChanged();
				}
			}
		}



		public long FirmwareVersion { get; private set; }
	}
}

