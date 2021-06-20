using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OATCommunications
{
    public class TCPSimServer
    {
        CultureInfo _oatCulture = new CultureInfo("en-US");

        int port = 8888;
        Socket serverSocket = null;
        public Socket tempClient = null;
        Task taskOfAccept;

        public void StartServer()
        {
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
                    if (dataReceive.EndsWith("\n") && dataReceive.StartsWith("Connect:"))
                    {
                        Send("OK:1\n");
                    }
                    else if(dataReceive.StartsWith("1#"))
                    {

                    }
                    dataReceive = string.Empty;

                }
                while (bytes > 0);
            }

        }
            
        public void Send(string command)
        {
            if(tempClient != null)
            {
                byte[] bytesSent = Encoding.ASCII.GetBytes(command);
                tempClient.Send(bytesSent, bytesSent.Length, 0); //Send is also blocked synchronously
            }
        }
            
        public void StopServer()
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

    }
}
