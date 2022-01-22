using OATCommunications.CommunicationHandlers;
using OATCommunications.Utilities;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;

namespace OATCommunications.WPF.CommunicationHandlers
{
	public class SerialCommunicationHandler : CommunicationHandler
	{
		private string _portName;
		private SerialPort _port;
		private List<string> _available;
		
		public SerialCommunicationHandler()
		{
			_available = new List<string>();
			_portName = string.Empty;
			_port = null;
		}

		public SerialCommunicationHandler(string comPort)
		{
			Log.WriteLine($"COMMFACTORY: Creating Serial handler on {comPort} ...");
			var regex = new System.Text.RegularExpressions.Regex(@"([A-z]+:\s*)(COM\d)?@?(\d+)?");
			var result =regex.Match(comPort);
			if (result.Success)
			{
				_portName = result.Groups[2].Value;
				_port = new SerialPort(_portName);
				int rate = 19200;
				if (result.Groups.Count==4)
				{
					int.TryParse(result.Groups[3].Value, out rate);
				}
				_port.BaudRate = rate;
				_port.DtrEnable = false;
				_port.ReadTimeout = 1000;
				_port.WriteTimeout = 1000;
			}
		}

		public override string Name => "Serial Port";

		public override bool Connected { get { return _port.IsOpen; } }

		long requestIndex = 1;

		protected override void RunJob(Job job)
		{
			CommandResponse response = null;

			if (_logJobs) Log.WriteLine("SERIAL: {0:0000}: [{1}] Processing Job", requestIndex, job.Command);
			if (Connected)
			{
				if (_logJobs) Log.WriteLine("SERIAL: {0:0000}: [{1}] Connected! Discarding any bytes in input buffer", requestIndex, job.Command);
				_port.DiscardInBuffer();
				requestIndex++;
				try
				{
					if (_logJobs) Log.WriteLine("SERIAL: {0:0000}: [{1}] Sending command", requestIndex, job.Command);
					_port.Write(job.Command);
				}
				catch (Exception ex)
				{
					Log.WriteLine("SERIAL: {0:0000}: [{1}] Failed to send command. {2}", requestIndex, job.Command, ex.Message);
					job.OnFulFilled(new CommandResponse(string.Empty, false, $"Unable to write to {_portName}. " + ex.Message));
					return;
				}

				try
				{
					switch (job.ResponseType)
					{
						case ResponseType.NoResponse:
							{
								if (_logJobs) Log.WriteLine("SERIAL: {0:0000}: [{1}] No response needed for command", requestIndex, job.Command);
								response = new CommandResponse(string.Empty, true);
							}
							break;

						case ResponseType.DigitResponse:
							{
								if (_logJobs) Log.WriteLine("SERIAL: {0:0000}: [{1}] Expecting single digit response for command, waiting...", requestIndex, job.Command);
								string responseStr = new string((char)_port.ReadChar(), 1);
								if (_logJobs) Log.WriteLine("SERIAL: {0:0000}: [{1}] Received single digit response '{2}' for command", requestIndex, job.Command, responseStr);
								response = new CommandResponse(responseStr, true);
							}
							break;

						case ResponseType.FullResponse:
							{
								if (_logJobs) Log.WriteLine("SERIAL: {0:0000}: [{1}] Expecting #-delimited response for Command, waiting...", requestIndex, job.Command);
								string responseStr = _port.ReadTo("#");
								if (_logJobs) Log.WriteLine("SERIAL: {0:0000}: [{1}] Received response '{2}' for command", requestIndex, job.Command, responseStr);
								response = new CommandResponse(responseStr, true);
							}
							break;
						case ResponseType.DoubleFullResponse:
							{
								if (_logJobs) Log.WriteLine("SERIAL: {0:0000}: [{1}] Expecting two #-delimited responses for Command, waiting for first...", requestIndex, job.Command);
								string responseStr = _port.ReadTo("#");
								if (_logJobs) Log.WriteLine("SERIAL: {0:0000}: [{1}] Received first response '{2}' for command", requestIndex, job.Command, responseStr);
								response = new CommandResponse(responseStr, true);
								responseStr = _port.ReadTo("#");
								if (_logJobs) Log.WriteLine("SERIAL: {0:0000}: [{1}] Received second response '{2}' for command, ignoring", requestIndex, job.Command, responseStr);
							}
							break;
					}
				}
				catch (Exception ex)
				{
					Log.WriteLine("SERIAL: {0:0000}: [{1}] Failed to receive response to command. {2}", requestIndex, job.Command, ex.Message);
					response = new CommandResponse(string.Empty, false, $"Unable to read response to {job.Command} from {_portName}. {ex.Message}");
				}
			}
			else
			{
				Log.WriteLine("SERIAL: {0:0000}: Port {1} is not open", requestIndex, _portName);
				response = new CommandResponse(string.Empty, false, $"Unable to open {_portName}");
			}

			if (_logJobs) Log.WriteLine("SERIAL: {0:0000}: Calling OnFulFilled lambda", requestIndex);
			job.OnFulFilled(response);
			if (_logJobs) Log.WriteLine("SERIAL: {0:0000}: Job completed", requestIndex);
		}

		public override bool Connect()
		{
			if (!_port.IsOpen)
			{
				try
				{
					Log.WriteLine("SERIAL: Port {0} is not open, attempting to open...", _portName);
					_port.Open();
					if (_port.IsOpen)
					{
						Log.WriteLine("SERIAL: Port is open, starting Jobs Processor.");
						StartJobsProcessor();
					}
					else
					{
						Log.WriteLine("SERIAL: Port did not open.");
					}
				}
				catch (Exception ex)
				{
					Log.WriteLine("SERIAL: Failed to open the port. {0}", ex.Message);
				}
			}
			return _port.IsOpen;
		}

		public override void Disconnect()
		{
			Log.WriteLine("SERIAL: Stopping Jobs processor.");
			StopJobsProcessor();
			if (_port != null && _port.IsOpen)
			{
				Log.WriteLine("SERIAL: Port is open, sending shutdown command [:Qq#]");
				if (_port.IsOpen)
				{
					_port.Write(":Qq#");
					Thread.Sleep(10);
					Log.WriteLine("SERIAL: Closing port...");
					_port.Close();
				}
				_port = null;
				Log.WriteLine("SERIAL: Disconnected...");
			}
		}

		public override void DiscoverDeviceInstances(Action<string> addDevice)
		{
			Log.WriteLine("SERIAL: Checking Serial ports....");

			_available.Clear();
			foreach (var port in SerialPort.GetPortNames())
			{
				Log.WriteLine("SERIAL: Found Serial port [{0}]", port);
				if (!_available.Contains("Serial: " + port))
				{
					_available.Add("Serial: " + port);
					addDevice("Serial: " + port);
				}
			}
		}

		public override bool IsDriverForDevice(string device)
		{
			return device.StartsWith("Serial: ");
		}

		public override ICommunicationHandler CreateHandler(string device)
		{
			return new SerialCommunicationHandler(device);
		}
	}
}
