using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using OATCommunications;
using OATCommunications.Model;
using OATCommunications.Utilities;
using OATCommunications.Avalonia;
using OATCommunications.Avalonia.CommunicationHandlers;

namespace OATTest
{
    public class TestVM : NotifyPropertyChanged
    {
        TestManager _testManager;
        ObservableCollection<string> _debugOutput;
        object _debugLock = new object();
        ICommunicationHandler? _handler;
        SerialListener? _debugHandler;
        AsyncAutoResetEvent _asyncAutoResetEvent = new AsyncAutoResetEvent();
        AsyncAutoResetEvent _singleStepSignal = new AsyncAutoResetEvent();
        CultureInfo _oatCulture = new CultureInfo("en-US");
        DeviceDriver? _commandDevice;
        DeviceDriver? _debugPort;
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
        private List<string> _baudRates = new();
        private string? _appStatus;
        private TestSuite? _selectedTestSuite;
        private CommandTest? _selectedTest;
        private int _succeeded;
        private int _failed;
        private int _skipped;
        private int _completed;
        private string? _succeededTests;
        private string? _failedTests;
        private string? _skippedTests;
        private string? _completedTests;
        private bool _stopOnError;
        private bool _canStep;
        private bool _canStop;

        // Injected by MainWindow to show confirmation dialogs
        public Func<string, Task<bool>>? ConfirmAction { get; set; }

        public ICommand SetDateTimeToPresetCommand => _setDateTimePresetCommand;
        public ICommand SetDateTimeToNowCommand => _setDateTimeCommand;
        public ICommand RunTestCommand => _runTestCommand;
        public ICommand DebugTestCommand => _debugTestCommand;
        public ICommand StopTestCommand => _stopTestCommand;
        public ICommand ContinueTestCommand => _continueTestCommand;
        public ICommand ResetTestsCommand => _resetTestsCommand;
        public ICommand ResetScanDevicesCommand => _resetScanDevicesCommand;
        public ICommand OpenLogsCommand => _openLogsCommand;

        public TestVM()
        {
            _useDateTime = DateTime.Parse("03/28/22 23:00:00", System.Globalization.CultureInfo.InvariantCulture);
            _presetTime = _useDateTime;
            TestSuites = new ObservableCollection<TestSuite>();

            CommunicationHandlerFactory.Initialize();
            _testManager = new TestManager();
            _seperateDebugPort = false;
            _debugOutput = new ObservableCollection<string>();
            OnPropertyChanged("Tests");
            Version = Assembly.GetExecutingAssembly().GetName().Version!;
            _debugOutput.Add("Welcome to TestManager " + Version);
            _setDateTimeCommand = new DelegateCommand(_ => OnSetDateTime(true));
            _setDateTimePresetCommand = new DelegateCommand(_ => OnSetDateTime(false));
            _runTestCommand = new DelegateCommand(async _ => await OnStartTest(false));
            _debugTestCommand = new DelegateCommand(async _ => await OnStartTest(true));
            _stopTestCommand = new DelegateCommand(_ => OnStopTest());
            _continueTestCommand = new DelegateCommand(_ => OnContinueTest());
            _resetTestsCommand = new DelegateCommand(() => OnResetTests());
            _resetScanDevicesCommand = new DelegateCommand(() => OnRescanDevices());
            _openLogsCommand = new DelegateCommand(() => OnOpenLogsFolder());

            _commandBaudRate = "19200";
            _debugBaudRate = "115200";

            AvailableDevices = new ObservableCollection<DeviceDriver>();
            AvailableBaudRates = new List<string>() { "9600", "19200", "28800", "38400", "57600", "115200" };


            _stopOnError = false;

            foreach (var testSuite in _testManager.TestSuites)
            {
                if (TestSuites.FirstOrDefault(t => t.Name == testSuite.Name) == null)
                    TestSuites.Add(testSuite);
            }
        }

        void OATDebug(string line) => Debug("OAT: " + line);

        public Version Version { get; private set; }

        void Debug(string line)
        {
            Log.WriteLine(line);
            AvaloniaUtilities.RunOnUiThread(() =>
            {
                lock (_debugLock)
                    _debugOutput.Add(line);
                OnPropertyChanged("LastLineIndex");
            });
        }

        public void OnOpenLogsFolder()
        {
            string sFolder = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OpenAstroTracker");
            Process.Start(new ProcessStartInfo { FileName = sFolder, UseShellExecute = true });
        }

        public void OnRescanDevices() => _ = OnDiscoverDevices();

        public void OnResetTests()
        {
            string? currentSuite = SelectedTestSuite?.Name;
            _testManager.ResetAllTests();
            TestSuites.Clear();
            foreach (var testSuite in _testManager.TestSuites)
            {
                if (TestSuites.FirstOrDefault(t => t.Name == testSuite.Name) == null)
                {
                    TestSuites.Add(testSuite);
                    if (testSuite.Name == currentSuite)
                        SelectedTestSuite = testSuite;
                }
            }
            _completed = 0;
            OnPropertyChanged("NumTests");
            AppStatus = string.Empty;
            SkippedTests = "-";
            SucceededTests = "-";
            FailedTests = "-";
            CompletedTests = "-";
            lock (_debugLock)
                _debugOutput.Clear();
            _testManager.PrepareForRun();
        }

        private void OnContinueTest() => _singleStepSignal.Set();

        public void Shutdown()
        {
            AppStatus = "Shutting down...";
            _testManager.AbortRun();
            _asyncAutoResetEvent.Set();
            _singleStepSignal.Set();
            CanStop = false;

            if (_debugHandler != null && _debugHandler.Connected)
            {
                _debugHandler.Disconnect();
                _debugHandler = null;
            }

            if (_handler != null && _handler.Connected)
            {
                _handler.Disconnect();
                _handler = null;
            }
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
            lock (_debugLock)
                _debugOutput.Clear();
            _asyncAutoResetEvent = new AsyncAutoResetEvent();

            await UpdateResults(null, CommandTest.StatusType.Ready, false);

            _failedToConnect = true;
            try
            {
                if (IsDebugPortSeperate && _debugPort != null)
                {
                    string debugPort = _debugPort.DeviceName + "@" + DebugBaudRate;
                    Debug($"TESTVM: Connecting Debug channel to OAT on {debugPort}...");
                    AppStatus = "Connecting debug channel to OAT ...";
                    _debugHandler = new SerialListener(debugPort, OATDebug);
                    _debugHandler.Connect();
                    await Task.Delay(500);
                }

                string port = _commandDevice!.DeviceName + "@" + "19200";
                Debug($"TESTVM: Connecting to OAT on {port}...");
                AppStatus = "Connecting to OAT ...";
                _handler = CommunicationHandlerFactory.ConnectToDevice(port);
                _handler!.Connect();
                await Task.Delay(3000);

                _handler.SendCommand(":GVN#", resultNr =>
                {
                    if (resultNr.Success)
                    {
                        var versionNumbers = resultNr.Data.Substring(1).Split('.');
                        if (versionNumbers.Length == 3)
                        {
                            try
                            {
                                FirmwareVersion = long.Parse(versionNumbers[0]) * 10000L
                                    + long.Parse(versionNumbers[1]) * 100L
                                    + long.Parse(versionNumbers[2]);
                                _testManager.FirmwareVersion = FirmwareVersion;
                                AppStatus = $"Connected to OAT {resultNr.Data}, running tests...";
                                _failedToConnect = false;
                            }
                            catch { Debug($"TESTVM: Unable to parse firmware version '{resultNr.Data}'"); }
                        }
                    }
                    _asyncAutoResetEvent.Set();
                });

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
                await _testManager.RunAllTests(
                    _handler!,
                    async (test, result) => await UpdateResults(test, result, debug),
                    s => Debug(s),
                    async msg => ConfirmAction != null ? await ConfirmAction(msg) : true);
                Debug($"TESTVM: Tests complete, disconnecting...");
            }
            else
            {
                Debug($"TESTVM: Failed to connect to OAT, no tests run.");
            }

            CanStop = false;
            AppStatus = "Disconnecting...";
            await Task.Delay(500);

            if (_debugHandler != null && _debugHandler.Connected)
            {
                _debugHandler.Disconnect();
                _debugHandler = null;
            }
            await Task.Delay(250);

            if (_handler != null && _handler.Connected)
            {
                _handler.Disconnect();
                _handler = null;
            }
            await Task.Delay(250);

            Debug($"TESTVM: Finished.");
            AppStatus = "Tests complete.";
            await UpdateResults(null, CommandTest.StatusType.Ready, false);
        }

        public CommandTest? SelectedTest
        {
            get => _selectedTest;
            set { _selectedTest = value; OnPropertyChanged(); }
        }

        public int LastLineIndex => _debugOutput.Count - 1;

        private async Task<bool> UpdateResults(CommandTest? test, CommandTest.StatusType result, bool debug)
        {
            bool ret = true;
            if (test != null) SelectedTest = test;
            if (result == CommandTest.StatusType.Running) return true;

            if (result == CommandTest.StatusType.Failed) { _failed++; _completed++; if (StopOnError) ret = false; }
            if (result == CommandTest.StatusType.Skipped) { _skipped++; _completed++; }
            if (result == CommandTest.StatusType.Success) { _succeeded++; _completed++; }
            if (result == CommandTest.StatusType.Complete) { _succeeded++; _completed++; }

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
        public List<string> AvailableBaudRates { get => _baudRates; set => _baudRates = value; }

        public bool CanStep
        {
            get => _canStep;
            set { if (value != _canStep) { _canStep = value; OnPropertyChanged(); } }
        }

        public bool CanStop
        {
            get => CanRun && _canStop;
            set { if (value != _canStop) { _canStop = value; OnPropertyChanged(); } }
        }

        public bool CanRun => (_commandDevice != null) && Tests.Any();
        public bool CanReset => true;

        void OnRunDeviceHandlerSetup(object handler, object p) { }

        public async Task OnDiscoverDevices()
        {
            string? previousCommandDevice = _commandDevice?.DeviceName;

            AvaloniaUtilities.RunOnUiThread(() => AvailableDevices.Clear());
            CommunicationHandlerFactory.DiscoverDevices();
            await Task.Delay(500);

            foreach (var device in CommunicationHandlerFactory.AvailableDevices)
            {
                var handler = CommunicationHandlerFactory.AvailableHandlers.First(h => h.IsDriverForDevice(device));
                var driver = new DeviceDriver(device, handler.SupportsSetupDialog,
                    new DelegateCommand(p => OnRunDeviceHandlerSetup(handler, p!)));
                AvaloniaUtilities.RunOnUiThread(() => AvailableDevices.Add(driver));
            }

            AvaloniaUtilities.RunOnUiThread(() =>
            {
                if (AvailableDevices.Count == 0)
                {
                    CommandDevice = null;
                    return;
                }

                var selectedDevice = !string.IsNullOrEmpty(previousCommandDevice)
                    ? AvailableDevices.FirstOrDefault(d => d.DeviceName == previousCommandDevice)
                    : null;

                CommandDevice = selectedDevice ?? AvailableDevices[0];
            });
        }

        public ObservableCollection<TestSuite> TestSuites { get; private set; }

        public TestSuite? SelectedTestSuite
        {
            get => _selectedTestSuite;
            set
            {
                if (_selectedTestSuite != value)
                {
                    _selectedTestSuite = value;
                    _testManager.SetActiveTestSuite(_selectedTestSuite?.Name ?? string.Empty);
                    OnPropertyChanged();
                    OnPropertyChanged("CanRun");
                    OnPropertyChanged("NumTests");
                }
            }
        }

        public IList<CommandTest> Tests => _testManager.Tests;
        public ObservableCollection<string> DebugOutput => _debugOutput;

        public DeviceDriver? CommandDevice
        {
            get => _commandDevice;
            set
            {
                if (_commandDevice != value)
                {
                    _commandDevice = value;
                    OnPropertyChanged();
                    OnPropertyChanged("CanRun");
                }
            }
        }

        public DeviceDriver? DebugPort
        {
            get => _debugPort;
            set { if (_debugPort != value) { _debugPort = value; OnPropertyChanged(); } }
        }

        public bool StopOnError
        {
            get => _stopOnError;
            set { if (_stopOnError != value) { _stopOnError = value; OnPropertyChanged(); } }
        }

        public bool IsDebugPortSeperate
        {
            get => _seperateDebugPort;
            set { if (_seperateDebugPort != value) { _seperateDebugPort = value; OnPropertyChanged(); } }
        }

        public string UseDate
        {
            get => _useDateTime.ToString("MM/dd/yy");
            set
            {
                if (DateTime.TryParse(value, out DateTime date))
                {
                    _useDateTime = new DateTime(date.Year, date.Month, date.Day,
                        _useDateTime.Hour, _useDateTime.Minute, _useDateTime.Second);
                    OnPropertyChanged();
                }
            }
        }

        public string UseTime
        {
            get => _useDateTime.ToString("HH:mm:ss");
            set
            {
                if (DateTime.TryParse(value, out DateTime time))
                {
                    _useDateTime = new DateTime(_useDateTime.Year, _useDateTime.Month, _useDateTime.Day,
                        time.Hour, time.Minute, time.Second);
                    OnPropertyChanged();
                }
            }
        }

        public string DebugBaudRate
        {
            get => _debugBaudRate;
            set { if (_debugBaudRate != value) { _debugBaudRate = value; OnPropertyChanged(); } }
        }

        public string CommandBaudRate
        {
            get => _commandBaudRate;
            set { if (_commandBaudRate != value) { _commandBaudRate = value; OnPropertyChanged(); OnPropertyChanged("CanRun"); } }
        }

        public string? AppStatus
        {
            get => _appStatus;
            set { if (_appStatus != value) { _appStatus = value; OnPropertyChanged(); OnPropertyChanged("NumTests"); } }
        }

        public string? SucceededTests
        {
            get => _succeededTests;
            set { if (_succeededTests != value) { _succeededTests = value; OnPropertyChanged(); } }
        }

        public string? FailedTests
        {
            get => _failedTests;
            set { if (_failedTests != value) { _failedTests = value; OnPropertyChanged(); } }
        }

        public string? SkippedTests
        {
            get => _skippedTests;
            set { if (_skippedTests != value) { _skippedTests = value; OnPropertyChanged(); } }
        }

        public string? CompletedTests
        {
            get => _completedTests;
            set { if (_completedTests != value) { _completedTests = value; OnPropertyChanged(); OnPropertyChanged("TestsCompleted"); } }
        }

        public long TestsCompleted => _completed;
        public long NumTests => Tests.Count;
        public long FirmwareVersion { get; private set; }
    }
}
