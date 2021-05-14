using OATCommunications.CommunicationHandlers;
using OATCommunications.Utilities;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OATCommunications.WPF.CommunicationHandlers
{
	public class SerialCommunicationHandler : CommunicationHandler
	{
		private string _portName;
		private SerialPort _port;

		public SerialCommunicationHandler(string comPort)
		{
			Log.WriteLine($"COMMFACTORY: Creating Serial handler on {comPort} ...");
			var parts = comPort.Split('@');
			if (parts.Length > 0)
			{
				_portName = parts[0];
				_port = new SerialPort(_portName);
				int rate = 19200;
				if (parts.Length > 1)
				{
					int.TryParse(parts[1], out rate);
				}
				_port.BaudRate = rate;
				_port.DtrEnable = false;
				_port.ReadTimeout = 1000;
				_port.WriteTimeout = 1000;
			}
		}

		public override bool Connected { get { return _port.IsOpen; } }

		long requestIndex = 1;

		protected override void RunJob(Job job)
		{
			CommandResponse response = null;

			if (_logJobs) Log.WriteLine("[{0:0000}] SERIAL: [{1}] Processing Job", requestIndex, job.Command);
			if (Connected)
			{
				if (_logJobs) Log.WriteLine("[{0:0000}] SERIAL: [{1}] Connected! Discarding any bytes in input buffer", requestIndex, job.Command);
				_port.DiscardInBuffer();
				requestIndex++;
				try
				{
					if (_logJobs) Log.WriteLine("[{0:0000}] SERIAL: [{1}] Sending command", requestIndex, job.Command);
					_port.Write(job.Command);
				}
				catch (Exception ex)
				{
					Log.WriteLine("[{0:0000}] SERIAL: [{1}] Failed to send command. {2}", requestIndex, job.Command, ex.Message);
					job.OnFulFilled(new CommandResponse(string.Empty, false, $"Unable to write to {_portName}. " + ex.Message));
					return;
				}

				try
				{
					switch (job.ResponseType)
					{
						case ResponseType.NoResponse:
							{
								if (_logJobs) Log.WriteLine("[{0:0000}] SERIAL: [{1}] No response needed for command", requestIndex, job.Command);
								response = new CommandResponse(string.Empty, true);
							}
							break;

						case ResponseType.DigitResponse:
							{
								if (_logJobs) Log.WriteLine("[{0:0000}] SERIAL: [{1}] Expecting single digit response for command, waiting...", requestIndex, job.Command);
								string responseStr = new string((char)_port.ReadChar(), 1);
								if (_logJobs) Log.WriteLine("[{0:0000}] SERIAL: [{1}] Received single digit response '{2}' for command", requestIndex, job.Command, responseStr);
								response = new CommandResponse(responseStr, true);
							}
							break;

						case ResponseType.FullResponse:
							{
								if (_logJobs) Log.WriteLine("[{0:0000}] SERIAL: [{1}] Expecting #-delimited response for Command, waiting...", requestIndex, job.Command);
								string responseStr = _port.ReadTo("#");
								if (_logJobs) Log.WriteLine("[{0:0000}] SERIAL: [{1}] Received response '{2}' for command", requestIndex, job.Command, responseStr);
								response = new CommandResponse(responseStr, true);
							}
							break;
						case ResponseType.DoubleFullResponse:
							{
								if (_logJobs) Log.WriteLine("[{0:0000}] SERIAL: [{1}] Expecting two #-delimited responses for Command, waiting for first...", requestIndex, job.Command);
								string responseStr = _port.ReadTo("#");
								if (_logJobs) Log.WriteLine("[{0:0000}] SERIAL: [{1}] Received first response '{2}' for command", requestIndex, job.Command, responseStr);
								response = new CommandResponse(responseStr, true);
								responseStr = _port.ReadTo("#");
								if (_logJobs) Log.WriteLine("[{0:0000}] SERIAL: [{1}] Received second response '{2}' for command, ignoring", requestIndex, job.Command, responseStr);
							}
							break;
					}
				}
				catch (Exception ex)
				{
					Log.WriteLine("[{0:0000}] SERIAL: [{1}] Failed to receive response to command. {2}", requestIndex, job.Command, ex.Message);
					response = new CommandResponse(string.Empty, false, $"Unable to read response to {job.Command} from {_portName}. {ex.Message}");
				}
			}
			else
			{
				Log.WriteLine("[{0:0000}] SERIAL: Port {1} is not open", requestIndex, _portName);
				response = new CommandResponse(string.Empty, false, $"Unable to open {_portName}");
			}

			if (_logJobs) Log.WriteLine("[{0:0000}] SERIAL: Calling OnFulFilled lambda", requestIndex);
			job.OnFulFilled(response);
			if (_logJobs) Log.WriteLine("[{0:0000}] SERIAL: Job completed", requestIndex);
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
	}
}
