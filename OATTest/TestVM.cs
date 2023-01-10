using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using OATCommunications;
using OATCommunications.Model;
using OATCommunications.Utilities;
using OATCommunications.WPF;
using OATCommunications.WPF.CommunicationHandlers;
using OATTest.Properties;

namespace OATTest
{
	public class TestVM : NotifyPropertyChanged
	{
		TestManager _testManager;
		ObservableCollection<string> _debugOutput;
		ICommunicationHandler _handler;
		SerialListener _debugHandler; // 
		AsyncAutoResetEvent _asyncAutoResetEvent = new AsyncAutoResetEvent();
		AsyncAutoResetEvent _singleStepSignal = new AsyncAutoResetEvent();
		CultureInfo _oatCulture = new CultureInfo("en-US");
		DeviceDriver _commandDevice;
		DeviceDriver _debugPort;
		bool _seperateDebugPort;
		DateTime _useDateTime;
		DateTime _presetTime;
		DelegateCommand _setDateTimePresetCommand;
		DelegateCommand _setDateTimeCommand;
		DelegateCommand _runTestCommand;
		DelegateCommand _stopTestCommand;
		DelegateCommand _debugTestCommand;
		DelegateCommand _continueTestCommand;
		DelegateCommand _resetTestsCommand;
		DelegateCommand _resetScanDevicesCommand;
		DelegateCommand _openLogsCommand;
		private bool _failedToConnect;
		private string _debugBaudRate;
		private string _commandBaudRate;
		private List<string> _baudRates;
		private string _appStatus;
		private TestSuite _selectedTestSuite;
		private CommandTest _selectedTest;
		private int _succeeded;
		private int _failed;
		private int _skipped;
		private int _completed;
		private string _succeededTests;
		private string _failedTests;
		private string _skippedTests;
		private string _completedTests;
		private bool _stopOnError;
		private bool _canStep;
		private bool _canStop;

		public ICommand SetDateTimeToPresetCommand { get { return _setDateTimePresetCommand; } }
		public ICommand SetDateTimeToNowCommand { get { return _setDateTimeCommand; } }
		public ICommand RunTestCommand { get { return _runTestCommand; } }
		public ICommand DebugTestCommand { get { return _debugTestCommand; } }
		public ICommand StopTestCommand { get { return _stopTestCommand; } }
		public ICommand ContinueTestCommand { get { return _continueTestCommand; } }
		public ICommand ResetTestsCommand { get { return _resetTestsCommand; } }
		public ICommand ResetScanDevicesCommand { get { return _resetScanDevicesCommand; } }
		public ICommand OpenLogsCommand { get { return _openLogsCommand; } }


		public TestVM()
		{
			_useDateTime = DateTime.Parse("03/28/22 23:00:00");
			_presetTime = _useDateTime;
			TestSuites = new ObservableCollection<TestSuite>();

			CommunicationHandlerFactory.Initialize();
			_testManager = new TestManager();
			_seperateDebugPort = false;
			_debugOutput = new ObservableCollection<string>();
			OnPropertyChanged("Tests");
			_debugOutput.Add("Welcome to TestManager V1.0");
			_setDateTimeCommand = new DelegateCommand(s => OnSetDateTime(true));
			_setDateTimePresetCommand = new DelegateCommand(s => OnSetDateTime(false));
			_runTestCommand = new DelegateCommand(async (s) => await OnStartTest(false));
			_debugTestCommand = new DelegateCommand(async (s) => await OnStartTest(true));
			_stopTestCommand = new DelegateCommand((s) => OnStopTest());
			_continueTestCommand = new DelegateCommand((s) => OnContinueTest());
			_resetTestsCommand = new DelegateCommand(() => OnResetTests());
			_resetScanDevicesCommand = new DelegateCommand(() => OnRescanDevices());
			_openLogsCommand = new DelegateCommand(() => OnOpenLogsFolder());

			_commandBaudRate = "19200";
			_debugBaudRate = "115200";

			AvailableDevices = new ObservableCollection<DeviceDriver>();
			AvailableBaudRates = new List<string>() { "9600", "19200", "28800", "38400", "57600", "115200" };

			Task.Run(async () => { await OnDiscoverDevices(); });

			_commandBaudRate = string.IsNullOrEmpty(Settings.Default.COMBaud) ? _commandBaudRate : Settings.Default.COMBaud;
			_debugBaudRate = string.IsNullOrEmpty(Settings.Default.DebugBaud) ? _debugBaudRate : Settings.Default.DebugBaud;
			_seperateDebugPort = Settings.Default.SeparateDebugPort;

			_stopOnError = Settings.Default.StopOnError;

			foreach (var testSuite in _testManager.TestSuites)
			{
				if (TestSuites.FirstOrDefault(t => t.Name == testSuite.Name) == null)
				{
					TestSuites.Add(testSuite);
					if (testSuite.Name == Settings.Default.Suite)
					{
						SelectedTestSuite = testSuite;
					}
				}
			}
		}

		void OATDebug(string line)
		{
			Debug("OAT: " + line);
		}

		void Debug(string line)
		{
			Log.WriteLine(line);
			WpfUtilities.RunOnUiThread(() =>
			{
				_debugOutput.Add(line);
				OnPropertyChanged("LastLineIndex");
			}, Application.Current.Dispatcher);
		}

		public void OnOpenLogsFolder()
		{
			string sFolder = string.Format("{0}\\OpenAstroTracker", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
			Process.Start(sFolder);
		}

		public void OnRescanDevices()
		{
			Task.Run(async () => { await OnDiscoverDevices(); });
		}

		public void OnResetTests()
		{
			string currentSuite = SelectedTestSuite?.Name;
			_testManager.ResetAllTests();
			TestSuites.Clear();
			foreach (var testSuite in _testManager.TestSuites)
			{
				if (TestSuites.FirstOrDefault(t => t.Name == testSuite.Name) == null)
				{
					TestSuites.Add(testSuite);
					if (testSuite.Name == (currentSuite ?? Settings.Default.Suite))
					{
						SelectedTestSuite = testSuite;
					}
				}
			}

			_completed = 0;
			OnPropertyChanged("NumTests");
			AppStatus = string.Empty;
			SkippedTests = "-";
			SucceededTests = "-";
			FailedTests = "-";
			CompletedTests = "-";
			_debugOutput.Clear();
			_testManager.PrepareForRun();
		}

		private void OnContinueTest()
		{
			_singleStepSignal.Set();
		}

		private void OnStopTest()
		{
			if (_testManager.AreTestsRunning)
			{
				AppStatus = "Aborting tests...";
				_testManager.AbortRun();
				_asyncAutoResetEvent.Set();
				_singleStepSignal.Set();
				CanStop = false;
			}
		}

		private async Task OnStartTest(bool debug)
		{
			Log.ReInit("OatTest");
			AppStatus = "Preparing tests...";
			_testManager.UseDate = _useDateTime;
			_testManager.ResetAllTests();
			_testManager.PrepareForRun();
			_succeeded = 0;
			_failed = 0;
			_skipped = 0;
			_completed = 0;
			_debugOutput.Clear();
			_asyncAutoResetEvent = new AsyncAutoResetEvent();

			await UpdateResults(null, CommandTest.StatusType.Ready, false);

			_failedToConnect = true;
			try
			{
				if (IsDebugPortSeperate)
				{
					string debugPort = DebugPort.DeviceName + "@" + DebugBaudRate;
					Debug($"TESTVM: Connecting Debug channel to OAT on {debugPort}...");
					AppStatus = "Connecting debug channel to OAT ...";

					// _debugHandler = CommunicationHandlerFactory.ConnectToDevice(debugPort);
					_debugHandler = new SerialListener(debugPort, this.OATDebug);
					_debugHandler.Connect();
					await Task.Delay(500);
				}

				string port = CommandDevice.DeviceName + "@" + "19200";
				Debug($"TESTVM: Connecting to OAT on {port}...");
				AppStatus = "Connecting to OAT ...";
				_handler = CommunicationHandlerFactory.ConnectToDevice(port);
				_handler.Connect();
				await Task.Delay(3000);

				_handler.SendCommand(":GVN#", (resultNr) =>
				{
					Debug($"TESTVM: OAT Version command replied ...");
					if (resultNr.Success)
					{
						var versionNumbers = resultNr.Data.Substring(1).Split(".".ToCharArray());
						if (versionNumbers.Length != 3)
						{
							Debug($"TESTVM: Unrecognizable firmware version '{resultNr.Data}'");
						}
						else
						{
							try
							{
								FirmwareVersion = long.Parse(versionNumbers[0]) * 10000L + long.Parse(versionNumbers[1]) * 100L + long.Parse(versionNumbers[2]);
								_testManager.FirmwareVersion = FirmwareVersion;

								AppStatus = $"Connected to OAT {resultNr.Data}, running tests...";

								Debug($"TESTVM: OAT is running firmware version '{resultNr.Data}' => {FirmwareVersion}");
								_failedToConnect = false;
							}
							catch
							{
								Debug($"TESTVM: Unable to parse firmware version '{resultNr.Data}'");
							}
						}
					}

					Debug($"TESTVM: Signalling OAT Version is done...");
					_asyncAutoResetEvent.Set();
				});

				Debug($"TESTVM: Await OAT Version...");
				await _asyncAutoResetEvent.WaitAsync();
			}
			catch (Exception ex)
			{
				Debug("TESTVM: Failed to connect. " + ex.Message);
			}

			if (!_failedToConnect)
			{
				Debug($"TESTVM: Connected to OAT with V{FirmwareVersion}, running tests...");
				CanStop = true;
				_testManager.PrepareForRun();
				_testManager.StopOnError = StopOnError;
				await _testManager.RunAllTests(_handler, async (test, result) => await UpdateResults(test, result, debug), (s) => Debug(s));

				Debug($"TESTVM: Tests complete, disconnecting...");
			}
			else
			{
				Debug($"TESTVM: Failed to connect to OAT, no tests run.");
			}

			CanStop = false;
			AppStatus = $"Disconnecting...";

			await Task.Delay(500);
			if (_debugHandler != null && _debugHandler.Connected)
			{
				_debugHandler.Disconnect();
				_debugHandler = null;
			}
			await Task.Delay(250);

			if (_handler.Connected)
			{
				_handler.Disconnect();
				_handler = null;
			}
			await Task.Delay(250);

			Debug($"TESTVM: Finished.");

			AppStatus = $"Tests complete.";

			await UpdateResults(null, CommandTest.StatusType.Ready, false);
		}



		public CommandTest SelectedTest
		{
			get { return _selectedTest; }
			set { _selectedTest = value; OnPropertyChanged(); }
		}

		public int LastLineIndex
		{
			get { return _debugOutput.Count - 1; }
		}



		private async Task<bool> UpdateResults(CommandTest test, CommandTest.StatusType result, bool debug)
		{
			bool ret = true;

			if (test != null)
			{
				SelectedTest = test;
			}
			if (result == CommandTest.StatusType.Running)
			{
				return true;
			}

			if (result == CommandTest.StatusType.Failed)
			{
				_failed++;
				_completed++;
				if (StopOnError)
				{
					ret = false;
				}
			}
			if (result == CommandTest.StatusType.Skipped)
			{
				_skipped++;
				_completed++;
			}
			if (result == CommandTest.StatusType.Success)
			{
				_succeeded++;
				_completed++;
			}
			if (result == CommandTest.StatusType.Complete)
			{
				_succeeded++;
				_completed++;
			}

			CompletedTests = $"{_completed}/{_testManager.Tests.Count} completed";
			FailedTests = $"{_failed} failed";
			SkippedTests = $"{_skipped} skipped";
			SucceededTests = $"{_succeeded} succeeded";
			if (debug && result != CommandTest.StatusType.Skipped)
			{
				CanStep = true;
				await _singleStepSignal.WaitAsync();
				CanStep = false;
			}
			return ret;
		}

		private void OnSetDateTime(bool useNow)
		{
			_useDateTime = useNow ? DateTime.Now : _presetTime;
			OnPropertyChanged("UseTime");
			OnPropertyChanged("UseDate");
		}

		public ObservableCollection<DeviceDriver> AvailableDevices { get; private set; }
		public List<string> AvailableBaudRates { get { return _baudRates; } set { _baudRates = value; } }

		public bool CanStep
		{
			get
			{
				return _canStep;
			}
			set
			{
				if (value != _canStep)
				{
					_canStep = value;
					OnPropertyChanged();
				}
			}
		}

		public bool CanStop
		{
			get
			{
				return CanRun && _canStop;
			}
			set
			{
				if (value != _canStop)
				{
					_canStop = value;
					OnPropertyChanged();
				}
			}
		}

		public bool CanRun
		{
			get
			{
				return (CommandDevice != null) && Tests.Any();
			}
		}

		public bool CanReset
		{
			get
			{
				return true;
			}
		}

		void OnRunDeviceHandlerSetup(object handler, object p) { }

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
			// WpfUtilities.RunOnUiThread(() => { AvailableDevices.Add("ASCOM: OAT"); }, Application.Current.Dispatcher);
			if (!string.IsNullOrEmpty(Settings.Default.CommandDevice))
			{
				WpfUtilities.RunOnUiThread(() => { CommandDevice = AvailableDevices.FirstOrDefault(d => d.DeviceName == Settings.Default.CommandDevice); }, Application.Current.Dispatcher);
			}
			if (!string.IsNullOrEmpty(Settings.Default.DebugPort))
			{
				WpfUtilities.RunOnUiThread(() => { DebugPort = AvailableDevices.FirstOrDefault(d => d.DeviceName == Settings.Default.DebugPort); }, Application.Current.Dispatcher);
			}
		}

		public ObservableCollection<TestSuite> TestSuites { get; private set; }
		public TestSuite SelectedTestSuite
		{
			get { return _selectedTestSuite; }
			set
			{
				if (_selectedTestSuite != value)
				{
					_selectedTestSuite = value;

					_testManager.SetActiveTestSuite(_selectedTestSuite?.Name ?? string.Empty);

					Settings.Default.Suite = SelectedTestSuite?.Name ?? string.Empty;
					Settings.Default.Save();
					OnPropertyChanged();
					OnPropertyChanged("CanRun");
					OnPropertyChanged("NumTests");
				}
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

		public DeviceDriver CommandDevice
		{
			get { return _commandDevice; }
			set
			{
				if (_commandDevice != value)
				{
					_commandDevice = value;
					//if (!_seperateDebugPort)
					//{
					//	DebugPort = value;
					//}
					Settings.Default.CommandDevice = _commandDevice.DeviceName;
					Settings.Default.Save();
					OnPropertyChanged();
					OnPropertyChanged("CanRun");
				}
			}
		}

		public DeviceDriver DebugPort
		{
			get { return _debugPort; }
			set
			{
				if (_debugPort != value)
				{
					_debugPort = value;
					Settings.Default.DebugPort = _debugPort.DeviceName;
					Settings.Default.Save();
					OnPropertyChanged();
				}
			}
		}

		public bool StopOnError
		{
			get { return _stopOnError; }
			set
			{
				if (_stopOnError != value)
				{
					_stopOnError = value;
					Settings.Default.StopOnError = StopOnError;
					Settings.Default.Save();
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
					//if (string.IsNullOrEmpty(DebugPort))
					//{
					//	DebugPort = CommandPort;
					//}
					OnPropertyChanged();
					Settings.Default.SeparateDebugPort= _seperateDebugPort;
					Settings.Default.Save();
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

		public string DebugBaudRate
		{
			get { return _debugBaudRate; }
			set
			{
				if (_debugBaudRate != value)
				{
					_debugBaudRate = value;
					OnPropertyChanged();
					Settings.Default.DebugBaud = DebugBaudRate;
					Settings.Default.Save();
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
					Settings.Default.COMBaud = CommandBaudRate;
					Settings.Default.Save();
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
					OnPropertyChanged("NumTests");
				}
			}
		}

		public string SucceededTests
		{
			get { return _succeededTests; }
			set
			{
				if (_succeededTests != value)
				{
					_succeededTests = value;
					OnPropertyChanged();
				}
			}
		}

		public string FailedTests
		{
			get { return _failedTests; }
			set
			{
				if (_failedTests != value)
				{
					_failedTests = value;
					OnPropertyChanged();
				}
			}
		}

		public string SkippedTests
		{
			get { return _skippedTests; }
			set
			{
				if (_skippedTests != value)
				{
					_skippedTests = value;
					OnPropertyChanged();
				}
			}
		}

		public string CompletedTests
		{
			get { return _completedTests; }
			set
			{
				if (_completedTests != value)
				{
					_completedTests = value;
					OnPropertyChanged();
					OnPropertyChanged("TestsCompleted");
				}
			}
		}

		public long TestsCompleted
		{
			get { return _completed; }
		}

		public long NumTests
		{
			get { return Tests.Count; }
		}



		public long FirmwareVersion { get; private set; }
	}
}

