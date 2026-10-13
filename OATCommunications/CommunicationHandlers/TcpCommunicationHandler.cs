using OATCommunications.ClientAdapters;
using OATCommunications.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using static OATCommunications.ClientAdapters.UdpClientAdapter;

namespace OATCommunications.CommunicationHandlers
{
	public class TcpCommunicationHandler : CommunicationHandler
	{
		private IPAddress _ip;
		private int _port;
		private TcpClient _client;
		private NetworkStream _stream;
		private List<string> _available;
		private Action<string> _addCallback;

		public TcpCommunicationHandler()
		{
			_port = 0;
			_available = new List<string>();
		}

		public TcpCommunicationHandler(string spec)
		{
			Log.WriteLine($"COMMFACTORY: Reading Wifi data from {spec} ...");

			var regex = new System.Text.RegularExpressions.Regex(@"([A-z]+:\s*)([A-z]+\s*)\(([0-9.]+):([0-9]+)\)@(\d+)");
			var result = regex.Match(spec);
			if (result.Success)
			{
				_ip = IPAddress.Parse(result.Groups[3].Value);
				_port = int.Parse(result.Groups[4].Value);
			}
			else
			{
				Log.WriteLine($"COMMFACTORY: Failed to parse IP address and port.");
			}
		}

		public override string Name => "TCP/IP";

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

				// Only (re)connect when the stream has been closed due to an error, or on first use.
				if (_stream == null)
				{
					try
					{
						_client = new TcpClient();
						_client.Connect(_ip, _port);
						_client.ReceiveTimeout = 1000;
						_client.SendTimeout = 1000;
						_stream = _client.GetStream();
					}
					catch (Exception e)
					{
						Log.WriteLine("TCP: [{0}] Failed To connect or create client for command: {1}", command, e.Message);
						job.OnFulFilled(new CommandResponse("", false, $"Failed To Connect to Client: {e.Message}"));
						return;
					}
				}

				string error = String.Empty;

				var bytes = Encoding.ASCII.GetBytes(command);
				try
				{
					_stream.Write(bytes, 0, bytes.Length);
					Log.WriteLine("TCP: [{0}] Sent command!", command);
				}
				catch (Exception e)
				{
					Log.WriteLine("TCP: [{0}] Unable to write command to stream: {1}", command, e.Message);
					_stream.Close();
					_stream = null;
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

						case ResponseType.DigitResponse:
						case ResponseType.FullResponse:
							{
								Log.WriteLine("TCP: [{0}] Expecting a {1} reply to command, waiting...", command, job.ResponseType.ToString());
								var response = new byte[256];
								var respCount = _stream.Read(response, 0, response.Length);
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

						case ResponseType.DoubleFullResponse:
							{
								Log.WriteLine("TCP: [{0}] Expecting a DoubleFullResponse reply to command, waiting...", command);
								var response = new byte[256];
								var respCount = _stream.Read(response, 0, response.Length);
								respString = Encoding.ASCII.GetString(response, 0, respCount);
								Log.WriteLine("TCP: [{0}] Received first reply to command -> [{1}], trimming", command, respString);
								int hashPos = respString.IndexOf('#');
								if (hashPos > 0)
								{
									respString = respString.Substring(0, hashPos);
								}
								Log.WriteLine("TCP: [{0}] Returning first reply to command -> [{1}]", command, respString);
								// Read and discard the second response
								var response2 = new byte[256];
								_stream.Read(response2, 0, response2.Length);
								attempt = 10;
							}
							break;
					}
				}
				catch (Exception e)
				{
					Log.WriteLine("TCP: [{0}] Failed to read reply to command. {1} thrown", command, e.GetType().Name);
					_stream.Close();
					_stream = null;
					respString = string.Empty;
				}

				attempt++;
				// Stream is intentionally left open for the next command.
			}

			bool succeeded = job.ResponseType == ResponseType.NoResponse || !string.IsNullOrEmpty(respString);
			job.OnFulFilled(new CommandResponse(respString, succeeded, succeeded ? string.Empty : $"Failed to read reply to [{command}]"));

		}

		public override bool Connected
		{
			get
			{
				return _client != null;
			}
		}

		public override bool Connect()
		{
			try
			{
				Log.WriteLine($"COMMFACTORY: Creating Wifi handler to monitor {_ip}:{_port} ...");
				_client = new TcpClient();
				Log.WriteLine("COMMFACTORY: Created TCP client, starting Jobs Processor ");
				StopJobsProcessor();
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
				var quitJob = new Job(99999, ":Qq#", ResponseType.NoResponse, (s) => { waitQuit.Set(); });
				RunJob(quitJob);
				waitQuit.WaitOne();

				Log.WriteLine("TCP: Closing port.");
				if (_stream != null)
				{
					_stream.Close();
					_stream = null;
				}
				_client.Close();
				_client = null;
				Log.WriteLine("TCP: Disconnected...");
			}
		}


		private void OnWifiClientFound(object sender, ClientFoundEventArgs e)
		{
			Log.WriteLine($"COMMFACTORY: Wifi device {e.Name} replied from {e.Address}:4031!");
			var deviceName = $"WiFi: {e.Name} ({e.Address}:4030)";
			_available.Insert(0, deviceName);
			_addCallback(deviceName);
		}

		public override bool IsDriverForDevice(string device)
		{
			return device.StartsWith("WiFi: ");
		}

		public override void DiscoverDeviceInstances(Action<string> addDevice)
		{
			_addCallback = addDevice;
			var searcher = new UdpClientAdapter("OAT", 4031);
			searcher.ClientFound += OnWifiClientFound;
			searcher.StartClientSearch();
		}

		public override ICommunicationHandler CreateHandler(string device)
		{
			return new TcpCommunicationHandler(device);
		}
	}
}
