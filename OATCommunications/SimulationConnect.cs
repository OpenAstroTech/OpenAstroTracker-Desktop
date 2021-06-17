using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


namespace OATCommunications
{
    public class SimulationConnect
    {
        private Socket _client = null;
        int port = 8888;
        Task taskOfReceiveData;

        public void Connect()
        {
            _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            var ipAddress = IPAddress.Parse("127.0.0.1");
            IPEndPoint ipe = new IPEndPoint(ipAddress, port);

            _client.Connect(ipAddress, 8888);
            if (_client.Connected)
            {
                string dataSent = "Connect:1\n";
                byte[] bytesSent = Encoding.ASCII.GetBytes(dataSent);
                _client.Send(bytesSent, bytesSent.Length, 0);
            }

            // taskOfReceiveData = new Task(receiveData);
            // taskOfReceiveData.Start();
        }

        public void Disconnect()
        {
            if(_client != null)
            {
                _client.Close();
            }
        }

        /*
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
                    dataReceive = "";
                }
                while (bytes > 0);
            }

        }
        */
        public void Send(string command)
        {
            byte[] bytesSent = Encoding.ASCII.GetBytes(command);
            _client.Send(bytesSent, bytesSent.Length, 0);
        }
    }
}
