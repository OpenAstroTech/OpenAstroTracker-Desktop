using OATSimulation.ViewModels;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace OATSimulation.Communication
{
    public class TCPSimClient
    {
        private Socket _client = null;
        Task taskOfReceiveData;
        MainViewModel _mainViewModel;
        private bool _isConnected = false;

        public bool IsConnected
        {
            get
            {
                return _isConnected;
            }

            set
            {
                _isConnected = value;
            }
        }

        public TCPSimClient(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }

        public void Connect(int port)
        {
            _mainViewModel.Status = "Trying to connect...";
            _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, port);

            for (var retries = 0; retries < 3; retries++)
            {
                try
                {
                    _client.Connect(endPoint);
                    if (_client.Connected)
                    {
                        string dataSent = "connect:1\n";
                        byte[] bytesSent = Encoding.ASCII.GetBytes(dataSent);
                        _client.Send(bytesSent, bytesSent.Length, 0);

                        _mainViewModel.Status = $"Connected on {IPAddress.Loopback}:{port}";
                        _mainViewModel.IsConnected = true;
                        _mainViewModel.IsConnectedString = "Disconnect";

                        taskOfReceiveData = new Task(receiveData);
                        taskOfReceiveData.Start();
                        break;
                    }
                }
                catch (SocketException)
                {
                    continue;
                }
            }

        }

        public void Disconnect()
        {
            if (_client != null)
            {
                Send("close\n");
                _mainViewModel.IsConnected = false;
                _mainViewModel.IsConnectedString = "Connect";
                _mainViewModel.Status = "Disconnected";
            }
        }

        private void receiveData()
        {
            while (true)
            {
                string dataReceive = string.Empty;
                byte[] bytesReceived = new byte[512];
                int bytes = 0;
                do
                {
                    bytes = _client.Receive(bytesReceived, bytesReceived.Length, 0);
                    dataReceive = dataReceive + Encoding.ASCII.GetString(bytesReceived, 0, bytes);
                    if (dataReceive.EndsWith("\n"))
                    {
                        ProcessCommand(dataReceive);
                    }
                    dataReceive = "";
                }
                while (bytes > 0);
            }

        }

        public void Send(string command)
        {
            byte[] bytesSent = Encoding.ASCII.GetBytes(command);
            _client.Send(bytesSent, bytesSent.Length, 0);
        }

        private void ProcessCommand(string command)
        {
            var cmdSplit = command.Split('\n');
            int intResult = 0;
            float floatResult = 0.0f;

            foreach (var cmd in cmdSplit)
            {
                var items = cmd.Split('|');

                switch (items[0])
                {
                    case "connect":
                        break;

                    case "ok":
                        break;

                    case "RAStepper":
                        bool success = int.TryParse(items[1], out intResult);
                        if (success)
                        {
                            _mainViewModel.RAStepper = intResult;
                        }
                        break;

                    case "DECStepper":
                        success = int.TryParse(items[1], out intResult);
                        if (success)
                        {
                            _mainViewModel.DECStepper = intResult;
                        }
                        break;

                    case "TrkStepper":
                        success = int.TryParse(items[1], out intResult);
                        if (success)
                        {
                            _mainViewModel.TRKStepper = intResult;
                        }
                        break;

                    case "RAStepsPerDegree":
                        success = float.TryParse(items[1], out floatResult);
                        if (success)
                        {
                            _mainViewModel.RAStepsPerDegree = floatResult;
                        }
                        break;

                    case "DECStepsPerDegree":
                        success = float.TryParse(items[1], out floatResult);
                        if (success)
                        {
                            _mainViewModel.DECStepsPerDegree = floatResult;
                        }
                        break;

                    case "Version":
                        _mainViewModel.Version = items[1];
                        break;

                    case "FirmwareVersion":
                        _mainViewModel.FirmwareVersion = items[1];
                        break;

                    case "ScopeSiderealTime":
                        _mainViewModel.ScopeSiderealTime = items[1];
                        break;

                    case "ScopePolarisHourAngle":
                        _mainViewModel.ScopePolarisHourAngle = items[1];
                        break;

                    case "CurrentRAString":
                        var _raValues = items[1].Split(',');
                        _mainViewModel.CurrentRAString = $"{_raValues[0]}h{_raValues[1]}m{_raValues[2]}s";
                        break;

                    case "CurrentDECString":
                        var _decValues = items[1].Split(',');
                        _mainViewModel.CurrentDECString = $"{_decValues[0]}\u00B0{_decValues[1]}m{_decValues[2]}s";
                        break;

                    case "ScopeRASlewMS":
                        success = int.TryParse(items[1], out intResult);
                        if (success)
                        {
                            _mainViewModel.ScopeRASlewMS = intResult;
                        }
                        break;
                    case "ScopeRATrackMS":
                        success = int.TryParse(items[1], out intResult);
                        if (success)
                        {
                            _mainViewModel.ScopeRATrackMS = intResult;
                        }
                        break;
                    case "ScopeDECSlewMS":
                        success = int.TryParse(items[1], out intResult);
                        if (success)
                        {
                            _mainViewModel.ScopeDECSlewMS = intResult;
                        }
                        break;
                    case "ScopeDECGuideMS":
                        success = int.TryParse(items[1], out intResult);
                        if (success)
                        {
                            _mainViewModel.ScopeDECGuideMS = intResult;
                        }
                        break;
                    case "ScopeLongitude":
                        _mainViewModel.ScopeLongitude = items[1];
                        break;
                    case "ScopeLatitude":
                        _mainViewModel.ScopeLatitude = items[1];
                        break;
                    case "ScopeTime":
                        _mainViewModel.ScopeTime = items[1];
                        break;
                    default:
                        break;
                }
                _mainViewModel.CalculatePositions();
            }
        }
    }
}
