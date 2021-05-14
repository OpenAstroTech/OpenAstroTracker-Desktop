using OATCommunications.Utilities;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace OATCommunications.CommunicationHandlers
{
	public class TcpCommunicationHandler : CommunicationHandler
	{
		private IPAddress _ip;
		private int _port;
		private TcpClient _client;

		public TcpCommunicationHandler(string spec)
		{
			Log.WriteLine($"COMMFACTORY: Reading Wifi data from {spec} ...");

			string ip = string.Empty;
			string port = string.Empty;

			var colon = spec.IndexOf(':');
			if (colon > 0)
			{
				try
				{
					ip = spec.Substring(0, colon);
					port = spec.Substring(colon + 1);
					_ip = IPAddress.Parse(ip);
					_port = int.Parse(port);
				}
				catch (Exception ex)
				{
					Log.WriteLine($"COMMFACTORY: Failed to parse IP address and port. {ex.Message}");
				}
			}
		}

		public TcpCommunicationHandler(IPAddress ip, int port)
		{
			Log.WriteLine($"COMMFACTORY: Reading Wifi data from {ip.ToString()}:{port} ...");
			_ip = ip;
			_port = port;
		}

		protected override void RunJob(Job job)
		{
			if (_client == null)
			{
				Log.WriteLine($"TCP: Configuration error, IP [{_ip}] or port [{_port}] is invalid.");
				job.OnFulFilled(new CommandResponse(string.Empty, false, $"Configuration error, IP [{_ip}] or port [{_port}] is invalid."));
				return;
			}

			int attempt = 1;
			var respString = String.Empty;
			string command = job.Command;

			while ((attempt < 4) && (_client != null))
			{
				Log.WriteLine("TCP: [{0}] Attempt {1} to send command.", command, attempt);
				if (!_client.Connected)
				{
					try
					{
						_client = new TcpClient();
						_client.Connect(_ip, _port);
					}
					catch (Exception e)
					{
						Log.WriteLine("TCP: [{0}] Failed To connect or create client for command: {1}", command, e.Message);
						job.OnFulFilled(new CommandResponse("", false, $"Failed To Connect to Client: {e.Message}"));
						return;
					}
				}

				_client.ReceiveTimeout = 1000;
				_client.SendTimeout = 1000;

				string error = String.Empty;

				var stream = _client.GetStream();
				var bytes = Encoding.ASCII.GetBytes(command);
				try
				{
					stream.Write(bytes, 0, bytes.Length);
					Log.WriteLine("TCP: [{0}] Sent command!", command);
				}
				catch (Exception e)
				{
					Log.WriteLine("TCP: [{0}] Unable to write command to stream: {1}", command, e.Message);
					job.OnFulFilled(new CommandResponse("", false, $"Failed to send message: {e.Message}"));
					return;
				}

				try
				{
					switch (job.ResponseType)
					{
						case ResponseType.NoResponse:
							attempt = 10;
							Log.WriteLine("TCP: [{0}] No reply needed to command", command);
							break;

						case ResponseType.DoubleFullResponse:
						case ResponseType.DigitResponse:
						case ResponseType.FullResponse:
							{
								Log.WriteLine("TCP: [{0}] Expecting a {1} reply to command, waiting...", command, job.ResponseType.ToString());
								var response = new byte[256];
								var respCount = stream.Read(response, 0, response.Length);
								respString = Encoding.ASCII.GetString(response, 0, respCount);
								Log.WriteLine("TCP: [{0}] Received reply to command -> [{1}], trimming", command, respString);
								int hashPos = respString.IndexOf('#');
								if (hashPos > 0)
								{
									respString = respString.Substring(0, hashPos);
								}
								Log.WriteLine("TCP: [{0}] Returning reply to command -> [{1}]", command, respString);
								attempt = 10;
							}
							break;
					}
				}
				catch (Exception e)
				{
					Log.WriteLine("TCP: [{0}] Failed to read reply to command. {1} thrown", command, e.GetType().Name);
					if (job.ResponseType != ResponseType.NoResponse)
					{
						respString = "0#";
					}
				}

				stream.Close();
				attempt++;
			}

			job.OnFulFilled(new CommandResponse(respString));
		}

		public override bool Connected
		{
			get
			{
				return _client != null && _client.Connected;
			}
		}

		public override bool Connect()
		{
			try
			{
				Log.WriteLine($"COMMFACTORY: Creating Wifi handler to monitor {_ip}:{_port} ...");
				_client = new TcpClient();
				Log.WriteLine("COMMFACTORY: Created TCP client, starting Jobs Processor ");
				StartJobsProcessor();
			}
			catch (Exception ex)
			{
				Log.WriteLine($"COMMFACTORY: Failed to create TCP client. {ex.Message}");
			}
			return true;
		}

		public override void Disconnect()
		{
			if (_client != null)
			{
				Log.WriteLine("TCP: Stopping Jobs processor.");
				StopJobsProcessor();

				Log.WriteLine("TCP: Port is open, sending shutdown command [:Qq#]");
				ManualResetEvent waitQuit = new ManualResetEvent(false);
				var quitJob = new Job(":Qq#", ResponseType.NoResponse, (s) => { waitQuit.Set(); });
				RunJob(quitJob);
				waitQuit.WaitOne();

				Log.WriteLine("TCP: Closing port.");
				_client.Close();
				_client = null;
				Log.WriteLine("TCP: Disconnected...");
			}
		}
	}
}