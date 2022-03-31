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
using OATCommunications.WPF;
using OATCommunications.WPF.CommunicationHandlers;

namespace OATTest
{
	public class TestVM : INotifyPropertyChanged
	{
		TestManager _testManager;
		ObservableCollection<string> _debugOutput;
		SerialCommunicationHandler _handler;
		CultureInfo _oatCulture = new CultureInfo("en-US");
		string _commandPort;
		string _debugPort;
		bool _seperateDebugPort;
		DateTime _useDateTime;
		DelegateCommand _setDateTimeCommand;
		DelegateCommand _runTestsCommand;

		public ICommand SetDateTimeToNowCommand { get{ return _setDateTimeCommand; } }

		public event PropertyChangedEventHandler PropertyChanged;

		public TestVM()
		{
			_useDateTime = DateTime.Parse("03/28/22 23:00:00");
			CommunicationHandlerFactory.Initialize();
			_testManager = new TestManager();
			_seperateDebugPort = false;
			_testManager.LoadTestSuite(@"c:\Users\Lutz.KRETZSCHMAR.000\Source\OpenAstroTracker-Desktop\OATTest\TestSuite.xml");
			_debugOutput = new ObservableCollection<string>();
			OnPropertyChanged("Tests");
			_debugOutput.Add("Welcome to TestManager V1.0");
			_debugOutput.Add("Test suite loaded");
			_setDateTimeCommand = new DelegateCommand(s => OnSetDateTime());
			_runTestsCommand = new DelegateCommand(s => OnStartTests());

			AvailableDevices = new ObservableCollection<string>();

			CommunicationHandlerFactory.DiscoverDevices();

			Task.Run(async () => { await OnDiscoverDevices(); });
		}

		private void OnStartTests()
		{
			_testManager.ResetAllTests();
			//_testManager.RunAllTests(() => { });
		}

		private void OnSetDateTime()
		{
			_useDateTime = DateTime.Now;
			OnPropertyChanged("UseTime");
			OnPropertyChanged("UseDate");
		}

		public ObservableCollection<string> AvailableDevices { get; private set; }

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

		void OnPropertyChanged([CallerMemberName] string prop = "")
		{
			var handler = this.PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(prop));
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
	}
}
