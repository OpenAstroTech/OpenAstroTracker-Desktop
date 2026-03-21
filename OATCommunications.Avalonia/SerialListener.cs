using OATCommunications.Utilities;
using System;
using System.IO.Ports;
using System.Text.RegularExpressions;

namespace OATCommunications.Avalonia
{
    public class SerialListener
    {
        private string _portName;
        private SerialPort? _port;
        private Action<string>? _serialOutput;
        private string _current = string.Empty;

        public SerialListener() { _portName = string.Empty; }

        public SerialListener(string comPort, Action<string> serialOutput)
        {
            _serialOutput = serialOutput;
            Log.WriteLine($"SERIAL: Creating Serial Listener on {comPort} ...");
            var result = Regex.Match(comPort, @"Serial:\s*([^@]+)@?(\d+)?");
            if (result.Success)
            {
                _portName = result.Groups[1].Value.Trim();
                _port = new SerialPort(_portName);
                int rate = 57600;
                if (!string.IsNullOrEmpty(result.Groups[2].Value))
                    int.TryParse(result.Groups[2].Value, out rate);
                _port.BaudRate = rate;
                _port.DtrEnable = false;
                _port.ReadTimeout = 1000;
                _port.WriteTimeout = 1000;
                _port.DataReceived += SerialDataReceived;
            }
            else
            {
                _portName = string.Empty;
            }
        }

        private void SerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (e.EventType == SerialData.Chars && _port != null)
            {
                _current += _port.ReadExisting();
                int eol = _current.IndexOfAny("\n\r".ToCharArray());
                if (eol >= 0)
                {
                    string output = _current.Substring(0, eol).Trim("\n\r\t ".ToCharArray());
                    if (!string.IsNullOrWhiteSpace(output))
                        _serialOutput?.Invoke(output);
                    _current = _current.Substring(eol + 1).Trim("\n\r\t ".ToCharArray());
                }
            }
        }

        public bool Connected => _port?.IsOpen ?? false;

        public bool Connect()
        {
            if (_port != null && !_port.IsOpen)
            {
                try
                {
                    Log.WriteLine("SERIAL: Port {0} is not open, attempting to open...", _portName);
                    _port.Open();
                }
                catch (Exception ex)
                {
                    Log.WriteLine("SERIAL: Failed to open the port. {0}", ex.Message);
                }
            }
            return _port?.IsOpen ?? false;
        }

        public void Disconnect()
        {
            Log.WriteLine("SERIAL: Disconnecting.");
            if (_port != null && _port.IsOpen)
            {
                _port.DataReceived -= SerialDataReceived;
                _port.Close();
                _port = null;
                Log.WriteLine("SERIAL: Disconnected...");
            }
        }
    }
}
