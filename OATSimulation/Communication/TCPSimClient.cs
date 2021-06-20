using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using OATSimulation.ViewModels;

namespace OATSimulation.Communication
{

    public class TCPSimClient
    {
        private Socket _client = null;
        int port = 8888;
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

        public void Connect()
        {
            _mainViewModel.Status = "Trying to connect...";
            _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var ipAddress = IPAddress.Parse("127.0.0.1");
            IPEndPoint endPoint = new IPEndPoint(ipAddress, port);

            for (var retries = 0; retries < 3; retries++)
            {
                try
                {
                    _client.Connect(endPoint);
                    if (_client.Connected)
                    {
                        string dataSent = "Connect:1\n";
                        byte[] bytesSent = Encoding.ASCII.GetBytes(dataSent);
                        _client.Send(bytesSent, bytesSent.Length, 0);

                        _mainViewModel.Status = "Connected";
                        _mainViewModel.IsConnected = true;
                        _mainViewModel.IsConnectedString = "Disconnect";
                        IsConnected = true;

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
                _client.Close();
                _mainViewModel.IsConnected = false;
                _mainViewModel.IsConnectedString = "Connect";
            }
        }


        private void receiveData()
        {
            while (true)
            {
                string dataReceive = string.Empty;
                byte[] bytesReceived = new byte[256];
                int bytes = 0;
                do
                {
                    bytes = _client.Receive(bytesReceived, bytesReceived.Length, 0);
                    dataReceive = dataReceive + Encoding.ASCII.GetString(bytesReceived, 0, bytes);
                    if (dataReceive.EndsWith("\n"))
                    {
                        ProcessCommand(dataReceive);
                        Send("1#");
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

            foreach (var cmd in cmdSplit)
            {
                var items = cmd.Split('|');

                switch (items[0])
                {
                    case "Connect":
                        break;

                    case "OK":
                        break;

                    case "RAStepper":
                        _mainViewModel.RAStepper = int.Parse(items[1]);
                        break;

                    case "DECStepper":
                        _mainViewModel.DECStepper = int.Parse(items[1]);
                        break;

                    case "TrkStepper":
                        _mainViewModel.TRKStepper = int.Parse(items[1]);
                        break;

                    case "RAStepsPerDegree":
                        _mainViewModel.RAStepsPerDegree = float.Parse(items[1]);
                        break;

                    case "DECStepsPerDegree":
                        _mainViewModel.DECStepsPerDegree = float.Parse(items[1]);
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

                    default:
                        break;
                }

                _mainViewModel.StatusUpdate();
            }
        }
    }
}
