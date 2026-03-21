using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using OATCommunications;
using OATCommunications.Avalonia;
using OATCommunications.Avalonia.CommunicationHandlers;
using OATCommunications.CommunicationHandlers;
using OATCommunications.Utilities;
using OATControl.ViewModels;

namespace OATControl.Avalonia.ViewModels
{
    public class ConnectVM : INotifyPropertyChanged
    {
        public enum Steps { Idle, WaitForDeviceConfirm, WaitForConnection, CheckHardware, WaitForGPS, ConfirmLocation, Completed }

        private Steps _currentStep = Steps.Idle;
        private DeviceDriverVM? _selectedDevice;
        private string _statusMessage = string.Empty;
        private bool _showStatus;
        private bool _showLocation;
        private bool _showNextButton;
        private bool _showRAHoming;
        private bool _showDECHoming;
        private bool _runRAAutoHoming;
        private bool _runDECOffsetHoming;
        private float _latitude = 45;
        private float _longitude = 0;
        private float _altitude = 100;
        private bool _decAutoHoming;
        private readonly CultureInfo _oatCulture = new CultureInfo("en-US");
        private DateTime _startedGPSWaitAt;
        private const float MaxWaitForGPS = 30.0f;

        private readonly Action<string, Action<CommandResponse>> _sendCommand;
        private readonly DispatcherTimer _stateTimer;

        // Injected from outside after connect succeeds
        public long FirmwareVersion { get; set; }
        public bool ScopeHasHSAH { get; set; }
        public bool ScopeHasHSAV { get; set; }
        public float DECStepperLowerLimit { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action? CloseRequested;

        public ConnectVM(Action<string, Action<CommandResponse>> sendCommand,
                         float savedLat, float savedLon, float savedAlt,
                         bool savedRunRAHoming, bool savedRunDECHoming)
        {
            _sendCommand = sendCommand;
            _latitude = savedLat;
            _longitude = savedLon;
            _altitude = savedAlt;
            _runRAAutoHoming = savedRunRAHoming;
            _runDECOffsetHoming = savedRunDECHoming;

            AvailableDevices = new ObservableCollection<DeviceDriverVM>();

            RescanCommand = new DelegateCommand(async () => await DiscoverDevices(), () => _currentStep == Steps.Idle);
            ConnectAndNextCommand = new DelegateCommand(() => AdvanceStateMachine(), () => IsNextEnabled);

            _stateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _stateTimer.Tick += (s, e) => ProcessStateMachine();

            Task.Run(async () => await DiscoverDevices());
            _stateTimer.Start();
        }

        public ICommand RescanCommand { get; }
        public ICommand ConnectAndNextCommand { get; }
        public ObservableCollection<DeviceDriverVM> AvailableDevices { get; }

        public bool? Result { get; set; }
        public bool RunRAAutoHoming => _runRAAutoHoming;
        public bool RunDECAutoHoming => _decAutoHoming ? _runDECOffsetHoming : false;
        public bool RunDECOffsetHoming => _runDECOffsetHoming;

        public DeviceDriverVM? SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (_selectedDevice != value)
                {
                    _selectedDevice = value;
                    if (_currentStep == Steps.Idle) AdvanceStateMachine();
                    OnPropertyChanged();
                    RequeryCommands();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool ShowStatus
        {
            get => _showStatus;
            set { _showStatus = value; OnPropertyChanged(); }
        }

        public bool ShowLocation
        {
            get => _showLocation;
            set { _showLocation = value; OnPropertyChanged(); }
        }

        public bool ShowNextButton
        {
            get => _showNextButton;
            set { _showNextButton = value; OnPropertyChanged(); }
        }

        public bool ShowRAHoming
        {
            get => _showRAHoming;
            set { _showRAHoming = value; OnPropertyChanged(); }
        }

        public bool ShowDECHoming
        {
            get => _showDECHoming;
            set { _showDECHoming = value; OnPropertyChanged(); }
        }

        public bool RunRAAutoHomingProp
        {
            get => _runRAAutoHoming;
            set { _runRAAutoHoming = value; OnPropertyChanged(); }
        }

        public bool RunDECOffsetHomingProp
        {
            get => _runDECOffsetHoming;
            set
            {
                _runDECOffsetHoming = value;
                OnPropertyChanged();
            }
        }

        public float Latitude
        {
            get => _latitude;
            set { _latitude = value; OnPropertyChanged(); }
        }

        public float Longitude
        {
            get => _longitude;
            set { _longitude = value; OnPropertyChanged(); }
        }

        public float Altitude
        {
            get => _altitude;
            set { _altitude = value; OnPropertyChanged(); }
        }

        public string DECHomingMethod => _decAutoHoming ? "Run DEC Auto-Home" : "Run DEC Offset Homing";

        public bool IsNextEnabled => _currentStep switch
        {
            Steps.Idle => false,
            Steps.CheckHardware => false,
            Steps.WaitForDeviceConfirm or Steps.WaitForConnection => _selectedDevice != null,
            _ => true
        };

        // Called by the dialog when user double-clicks a device
        public void OnDeviceDoubleClick()
        {
            if (_selectedDevice == null) return;
            _currentStep = _selectedDevice.Inner.DeviceName.StartsWith("Serial")
                ? Steps.WaitForDeviceConfirm
                : Steps.WaitForConnection;
            AdvanceStateMachine();
        }

        public async Task DiscoverDevices()
        {
            AvaloniaUtilities.RunOnUiThread(() => AvailableDevices.Clear());
            CommunicationHandlerFactory.DiscoverDevices();
            await Task.Delay(500);

            foreach (var device in CommunicationHandlerFactory.AvailableDevices)
            {
                var handler = CommunicationHandlerFactory.AvailableHandlers.FirstOrDefault(h => h.IsDriverForDevice(device));
                if (handler == null) continue;
                var driver = new DeviceDriverVM(new DeviceDriver(device, handler.SupportsSetupDialog,
                    new DelegateCommand(_ => handler.RunSetupDialog())));
                AvaloniaUtilities.RunOnUiThread(() => AvailableDevices.Add(driver));
            }
        }

        // Called from outside after ConnectToOat succeeds, to inject hardware info
        public void OnConnected(long firmwareVersion, bool hasHSAH, bool hasHSAV, float decLowerLimit)
        {
            FirmwareVersion = firmwareVersion;
            ScopeHasHSAH = hasHSAH;
            ScopeHasHSAV = hasHSAV;
            DECStepperLowerLimit = decLowerLimit;
            _decAutoHoming = hasHSAV;
        }

        // Raised when the dialog needs the owner to actually connect to the OAT
        public Func<string, Task<(bool success, string message)>>? ConnectToOatFunc { get; set; }

        private void AdvanceStateMachine()
        {
            switch (_currentStep)
            {
                case Steps.Idle:
                    _currentStep = Steps.WaitForDeviceConfirm;
                    ShowNextButton = true;
                    break;

                case Steps.WaitForDeviceConfirm:
                    _currentStep = Steps.CheckHardware;
                    StatusMessage = $"Connecting to OAT on {_selectedDevice!.Inner.DeviceName}...";
                    ShowStatus = true;
                    ShowNextButton = false;
                    break;

                case Steps.WaitForConnection:
                    _currentStep = Steps.CheckHardware;
                    StatusMessage = $"Connecting to OAT on {_selectedDevice!.Inner.DeviceName}...";
                    ShowStatus = true;
                    break;

                case Steps.CheckHardware:
                    // handled by timer
                    break;

                case Steps.WaitForGPS:
                    // User skipped GPS — fall through to manual location
                    ShowLocation = true;
                    _currentStep = Steps.ConfirmLocation;
                    StatusMessage = "GPS acquisition cancelled, please enter location:";
                    break;

                case Steps.ConfirmLocation:
                    StatusMessage = "Setting OAT location...";
                    Task.Run(async () =>
                    {
                        await SetLocation();
                        AvaloniaUtilities.RunOnUiThread(() =>
                        {
                            _currentStep = Steps.Completed;
                            AdvanceStateMachine();
                        });
                    });
                    break;

                case Steps.Completed:
                    Result = true;
                    _stateTimer.Stop();
                    CloseRequested?.Invoke();
                    break;
            }

            RequeryCommands();
        }

        private async void ProcessStateMachine()
        {
            _stateTimer.Stop();

            if (_currentStep == Steps.CheckHardware && ConnectToOatFunc != null)
            {
                var deviceName = _selectedDevice!.Inner.DeviceName;
                var (success, message) = await ConnectToOatFunc(deviceName);

                ShowStatus = true;
                if (!success)
                {
                    StatusMessage = message;
                    _currentStep = Steps.WaitForConnection;
                    ShowNextButton = _selectedDevice != null;
                    RequeryCommands();
                    _stateTimer.Start();
                    return;
                }

                // Connected — check for GPS addon
                bool hasGPS = false;
                var gpsEvent = new SemaphoreSlim(0, 1);
                _sendCommand("XGM#,#", r =>
                {
                    if (r.Success) hasGPS = r.Data.Contains("GPS");
                    gpsEvent.Release();
                });
                await gpsEvent.WaitAsync();

                if (hasGPS)
                {
                    _currentStep = Steps.WaitForGPS;
                    ShowStatus = true;
                    StatusMessage = "Waiting for GPS to find satellites and sync...";
                    ShowNextButton = true;
                    _startedGPSWaitAt = DateTime.UtcNow;
                }
                else
                {
                    ShowLocation = true;
                    _currentStep = Steps.ConfirmLocation;
                    ShowStatus = false;
                    ShowNextButton = true;
                }

                RequeryCommands();
            }

            if (_currentStep == Steps.WaitForGPS)
            {
                var elapsed = DateTime.UtcNow - _startedGPSWaitAt;
                var remaining = MaxWaitForGPS - elapsed.TotalSeconds;

                if (remaining <= 0)
                {
                    StatusMessage = "GPS could not get a location lock. Please enter location manually:";
                    ShowLocation = true;
                    _currentStep = Steps.ConfirmLocation;
                    RequeryCommands();
                    _stateTimer.Start();
                    return;
                }

                StatusMessage = $"Waiting {remaining:0}s for GPS to find satellites and sync...";
                ShowStatus = true;
                ShowNextButton = true;
            }

            _stateTimer.Start();
        }

        private void FetchStoredLocation()
        {
            float lat = 0, lng = 0;
            _sendCommand(":Gt#,#", a => { if (a.Success) TryParseLatLong(a.Data, ref lat); });
            _sendCommand(":Gg#,#", a =>
            {
                if (a.Success) TryParseLatLong(a.Data, ref lng);
                float correctedLng = FirmwareVersion < 11105 ? 180.0f - lng : -lng;
                AvaloniaUtilities.RunOnUiThread(() => { Latitude = lat; Longitude = correctedLng; });
            });
        }

        private async Task SetLocation()
        {
            var latDone = new SemaphoreSlim(0, 1);
            var lonDone = new SemaphoreSlim(0, 1);

            // Latitude: +/-DD*MM
            char latSign = _latitude < 0 ? '-' : '+';
            float absLat = Math.Abs(_latitude);
            int latDeg = (int)absLat;
            int latMin = (int)((absLat - latDeg) * 60.0f);
            _sendCommand($":St{latSign}{latDeg:00}*{latMin:00}#,n", _ => latDone.Release());

            // Longitude (input is -180 (W) to +180 (E))
            // Firmware < 1.11.05 expects 0..360 without sign; newer firmware expects signed inverted value.
            float fwLongitude = _longitude;
            if (FirmwareVersion < 11105)
            {
                fwLongitude = 180.0f - fwLongitude;
            }
            else
            {
                fwLongitude = -fwLongitude;
            }

            char lonSign = fwLongitude < 0 ? '-' : '+';
            float absLon = Math.Abs(fwLongitude);
            int lonDeg = (int)absLon;
            int lonMin = (int)((absLon - lonDeg) * 60.0f);
            string lonCommand = FirmwareVersion < 11105
                ? $":Sg{lonDeg:000}*{lonMin:00}#,n"
                : $":Sg{lonSign}{lonDeg:000}*{lonMin:00}#,n";
            _sendCommand(lonCommand, _ => lonDone.Release());

            await latDone.WaitAsync();
            await lonDone.WaitAsync();

            // Persist location to app settings for next session
            AppSettings.Instance.SiteLatitude = _latitude;
            AppSettings.Instance.SiteLongitude = _longitude;
            AppSettings.Instance.SiteAltitude = _altitude;
            AppSettings.Instance.Save();
        }

        private bool TryParseLatLong(string latlong, ref float result)
        {
            var parts = latlong.Split('*', '\'', ':');
            if (parts.Length < 2) return false;
            if (parts[0][0] == '-')
                result = -1.0f * (int.Parse(parts[0].Substring(1)) + int.Parse(parts[1]) / 60.0f);
            else
                result = int.Parse(parts[0]) + int.Parse(parts[1]) / 60.0f;
            return true;
        }

        private void RequeryCommands()
        {
            ((DelegateCommand)RescanCommand).Requery();
            ((DelegateCommand)ConnectAndNextCommand).Requery();
            OnPropertyChanged(nameof(IsNextEnabled));
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
