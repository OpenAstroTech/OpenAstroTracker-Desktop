using OATSimulation.ViewModels;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace OATSimulation.TCPServer
{
    class TCPServer
    {
        CultureInfo _oatCulture = new CultureInfo("en-US");

        int port = 8888;
        Socket serverSocket = null;
        Socket tempClient = null;
        Task taskOfAccept;
        
        MainViewModel _mainViewModel;

        public void StartServer(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            var address = IPAddress.Parse("127.0.0.1");
            IPEndPoint ipe = new IPEndPoint(address, port);
            serverSocket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(ipe);
            serverSocket.Listen(10);
            taskOfAccept = new Task(AcceptClient);
            taskOfAccept.Start();

        }

        private void AcceptClient()
        {
            while (true)
            {
                tempClient = serverSocket.Accept();
                ThreadPool.QueueUserWorkItem(new WaitCallback(Receive), tempClient);
                _mainViewModel.Status = "Connected";
            }
        }

        private void Receive(object state)
        {
            while (true)
            {
                string dataReceive = string.Empty;
                byte[] bytesReceived = new byte[256];
                int bytes = 0;
                do
                {
                    bytes = tempClient.Receive(bytesReceived, bytesReceived.Length, 0);

                    dataReceive = dataReceive + Encoding.ASCII.GetString(bytesReceived, 0, bytes);
                    if(dataReceive.EndsWith("\n"))
                    {
                        ProcessCommand(dataReceive);
                    }
                    dataReceive = string.Empty;

                }
                while (bytes > 0);
            }

        }

        /*
        public void Send(string command)
        {
            byte[] bytesSent = Encoding.ASCII.GetBytes(command);
            tempClient.Send(bytesSent, bytesSent.Length, 0); //Send is also blocked synchronously
        }
        */

        private void StopServer()
        {
            try
            {
                serverSocket.Shutdown(SocketShutdown.Both);
            }
            finally
            {
                serverSocket.Close();
            }
        }

        private void ProcessCommand(string command)
        {
            var cmdSplit = command.Split('\n');
            
            foreach (var cmd in cmdSplit)
            {
                var items = cmd.Split(':');

                switch (items[0])
                {
                    case "Connect":
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

                    default:
                        break;
                }

                _mainViewModel.StatusUpdate();
            }
        }

    }
}
