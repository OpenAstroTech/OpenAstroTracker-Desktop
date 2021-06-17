using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using OATSimulation.ViewModels;


namespace OATSimulation.TCPServer
{
    class TCPServer
    {
        CultureInfo _oatCulture = new CultureInfo("en-US");

        //string host;
        int port = 8888;
        System.Net.Sockets.Socket serverSocket = null;
        //IPHostEntry hostEntry = null;
        System.Net.Sockets.Socket tempClient = null;
        Task taskOfAccept;
        //Task taskOfReceive;

        MainViewModel _mainViewModel;

        public void StartServer(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;

            //var ipAddress = Dns.GetHostEntry("localhost").AddressList[0];
            //host = Dns.GetHostName();
            //hostEntry = Dns.GetHostEntry(host);

            //IPAddress address = hostEntry.AddressList[1];
            var address = IPAddress.Parse("127.0.0.1");
            IPEndPoint ipe = new IPEndPoint(address, port);
            serverSocket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);//Create a socket with which protocol
            serverSocket.Bind(ipe);
            serverSocket.Listen(10);
            // listbox.Dispatcher.BeginInvoke(new Action(() => { this.listbox.Items.Add(ipe.Address + "Server opened"); }));
            taskOfAccept = new Task(AcceptClient);
            taskOfAccept.Start();

        }

        private void AcceptClient()
        {
            while (true)
            {
                tempClient = serverSocket.Accept();//Continuous monitoring
                // listbox.Dispatcher.BeginInvoke(new Action(() => { this.listbox.Items.Add(" connected"); }));
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
                // The following will block until the page is transmitted.
                do
                {
                    bytes = tempClient.Receive(bytesReceived, bytesReceived.Length, 0);

                    dataReceive = dataReceive + Encoding.ASCII.GetString(bytesReceived, 0, bytes);
                    if(dataReceive.EndsWith("\n"))
                    {
                        ProcessCommand(dataReceive);
                    }
                    // listbox.Dispatcher.Invoke(new Action(() => { this.listbox.Items.Add("Server Received:" + dataReceive); }));
                    dataReceive = string.Empty;

                }
                while (bytes > 0); //If bytes <0 is exiting a client, this can be used to detect the survival status of the client
            }

        }

        /*
        private void Send_Click(object sender, RoutedEventArgs e)
        {
            string dataSend = string.Empty;
            dataSend = "hello client";
            byte[] bytesSent = Encoding.ASCII.GetBytes(dataSend);
            tempClient.Send(bytesSent, bytesSent.Length, 0); //Send is also blocked synchronously
            // listbox.Dispatcher.Invoke(new Action(() => { this.listbox.Items.Add("Server send:" + dataSend); }));
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
