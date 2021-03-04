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
			Log.WriteLine($"COMMFACTORY: Creating Serial handler on {comPort} at 57600 baud...");

			_portName = comPort;
			_port = new SerialPort(comPort);
			_port.BaudRate = 57600;
			_port.DtrEnable = false;
			_port.ReadTimeout = 1000;
			_port.WriteTimeout = 1000;

			StartJobsProcessor();
		}

		public override bool Connected { get { return _port.IsOpen; } }

		long requestIndex = 1;

		protected override void RunJob(Job job)
		{
			CommandResponse response = null;

			if (EnsurePortIsOpen())
			{
				_port.DiscardInBuffer();
				requestIndex++;
				try
				{
					Log.WriteLine("[{0:0000}] SERIAL: [{1}] Sending command", requestIndex, job.Command);
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
								Log.WriteLine("[{0:0000}] SERIAL: [{1}] No response needed for command", requestIndex, job.Command);
								response = new CommandResponse(string.Empty, true);
							}
							break;

						case ResponseType.DigitResponse:
							{
								Log.WriteLine("[{0:0000}] SERIAL: [{1}] Expecting single digit response for command, waiting...", requestIndex, job.Command);
								string responseStr = new string((char)_port.ReadChar(), 1);
								Log.WriteLine("[{0:0000}] SERIAL: [{1}] Received single digit response '{2}' for command", requestIndex, job.Command, responseStr);
								response = new CommandResponse(responseStr, true);
							}
							break;

						case ResponseType.FullResponse:
							{
								Log.WriteLine("[{0:0000}] SERIAL: [{1}] Expecting #-delimited response for Command, waiting...", requestIndex, job.Command);
								string responseStr = _port.ReadTo("#");
								Log.WriteLine("[{0:0000}] SERIAL: [{1}] Received response '{2}' for command", requestIndex, job.Command, responseStr);
								response = new CommandResponse(responseStr, true);
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
				Log.WriteLine("[{0:0000}] SERIAL: Failed to open port {1}", requestIndex, _portName);
				response = new CommandResponse(string.Empty, false, $"Unable to open {_portName}");
			}

			job.OnFulFilled(response);
		}

		private bool EnsurePortIsOpen()
		{
			if (!_port.IsOpen)
			{
				int attempts = 0;
				do
				{
					attempts++;
					try
					{
						Log.WriteLine("SERIAL: Port {0} is not open, attempt {1} to open...", _portName, attempts);
						_port.Open();
						Thread.Sleep(750); // Arduino resets on connection. Give it time to start up.
						Log.WriteLine("SERIAL: Port is open, sending initial [:I#] command..");
						_port.Write(":I#");
						return true;
					}
					catch (Exception ex)
					{
						Log.WriteLine("SERIAL: Failed to open the port on attempt {1}. {0}", ex.Message, attempts);
						Thread.Sleep(250); // Wait a little before trying again.
					}
				}
				while (attempts < 10);
				return false;
			}
			return true;
		}

		public override void Disconnect()
		{
			if (_port.IsOpen)
			{
				Log.WriteLine("SERIAL: Stopping Jobs processor.");
				StopJobsProcessor();
				Log.WriteLine("SERIAL: Port is open, sending shutdown command [:Qq#]");
				_port.Write(":Qq#");
				Log.WriteLine("SERIAL: Closing port...");
				_port.Close();
				_port = null;
				Log.WriteLine("SERIAL: Disconnected...");
			}
		}
	}
}
