using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OATCommunications
{
    public delegate void Notify();  // delegate
    /// <summary>
    /// Server class to handle communication with OAT Simulation client
    /// </summary>
    public class SimulationServer
    {
        Socket serverSocket = null;
        public Socket tempClient = null;
        Task taskOfAccept;

        public event Notify ClientConnected;
        
        public delegate void ClientCommandHandler(string cmd);
        public event ClientCommandHandler ClientCommand;

        public bool _isClientConnected = false;

        public bool IsClientConnected
        {
            get { return _isClientConnected; }
            set { _isClientConnected = value; }
        }

        public void Start(int port)
        {
            IPEndPoint ipe = new IPEndPoint(IPAddress.Loopback, port);
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
                try
                {
                    tempClient = serverSocket.Accept();
                    ThreadPool.QueueUserWorkItem(new WaitCallback(Receive), tempClient);
                }
                catch(SocketException e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        private void Receive(object state)
        {
            try
            {
                string dataReceive = string.Empty;
                byte[] bytesReceived = new byte[512];
                int bytes = 0;
                do
                {
                    bytes = tempClient.Receive(bytesReceived, bytesReceived.Length, 0);

                    dataReceive = dataReceive + Encoding.ASCII.GetString(bytesReceived, 0, bytes);
                    if (dataReceive.EndsWith("\n") && dataReceive.StartsWith("connect:"))
                    {
                        Send("ok:1\n");
                        IsClientConnected = true;
                        ClientConnected?.Invoke();

                    }
                    else if (dataReceive == "close\n")
                    {
                        tempClient.Close();
                        IsClientConnected = false;
                        return;
                    }
                    else if (dataReceive.StartsWith(":"))
                    {
                        ClientCommand?.Invoke(dataReceive);
                    }
                    dataReceive = string.Empty;

                }
                while (bytes > 0);
            }
            catch(SocketException e)
            {
                IsClientConnected = false;
                Console.WriteLine("Client disconnected...");
            }
        }
            
        public void Send(string command)
        {
            if(tempClient != null)
            {
                byte[] bytesSent = Encoding.ASCII.GetBytes(command+"\n");
                tempClient.Send(bytesSent, bytesSent.Length, 0);
            }
        }
            
        public void Stop()
        {
            try
            {
                serverSocket.Shutdown(SocketShutdown.Both);
            }
            catch(SocketException e)
            {
                
            }
            finally
            {
                serverSocket.Close();
                IsClientConnected = false;
            }
        }

    }
}
