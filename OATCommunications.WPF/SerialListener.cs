using OATCommunications.Utilities;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OATCommunications.WPF
{
	public class SerialListener
	{
		private string _portName;
		private SerialPort _port;
		private Action<string> _serialOutput;
		private string _current = string.Empty;

		public SerialListener()
		{
			_portName = string.Empty;
			_port = null;
		}

		public SerialListener(string comPort, Action<string> serialOutput)
		{
			_serialOutput = serialOutput;
			Log.WriteLine($"SERIAL: Creating Serial Listener on {comPort} ...");
			var regex = new System.Text.RegularExpressions.Regex(@"([A-z]+:\s*)(COM\d+)?@?(\d+)?");
			var result = regex.Match(comPort);
			if (result.Success)
			{
				_portName = result.Groups[2].Value;
				_port = new SerialPort(_portName);
				int rate = 57600;
				if (result.Groups.Count == 4)
				{
					int.TryParse(result.Groups[3].Value, out rate);
				}
				_port.BaudRate = rate;
				_port.DtrEnable = false;
				_port.ReadTimeout = 1000;
				_port.WriteTimeout = 1000;
				_port.DataReceived += SerialDataReceived;
			}
		}

		private void SerialDataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			if (e.EventType == SerialData.Chars)
			{
				_current += _port.ReadExisting();
				int eol = _current.IndexOfAny("\n\r".ToCharArray());
				if (eol >= 0)
				{
					string output = _current.Substring(0, eol).Trim("\n\r\t ".ToCharArray());
					if (!string.IsNullOrWhiteSpace(output))
					{
						_serialOutput(output);
					}
					_current = _current.Substring(eol + 1).Trim("\n\r\t ".ToCharArray());
				}
			}
		}

		public bool Connected { get { return _port.IsOpen; } }

		public bool Connect()
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
						// StartListening();
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

		public void Disconnect()
		{
			Log.WriteLine("SERIAL: Disconnecting.");
			if (_port != null && _port.IsOpen)
			{
				_port.DataReceived -= SerialDataReceived;
				Log.WriteLine("SERIAL: Port is open");
				if (_port.IsOpen)
				{
					Log.WriteLine("SERIAL: Closing port...");
					_port.Close();
				}
				_port = null;
				Log.WriteLine("SERIAL: Disconnected...");
			}
		}

	}
}
