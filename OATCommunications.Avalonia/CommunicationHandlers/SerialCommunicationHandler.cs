using OATCommunications.CommunicationHandlers;
using OATCommunications.Utilities;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Threading;

namespace OATCommunications.Avalonia.CommunicationHandlers
{
    public class SerialCommunicationHandler : CommunicationHandler
    {
        private string _portName;
        private SerialPort? _port;
        private List<string> _available;

        public SerialCommunicationHandler()
        {
            _available = new List<string>();
            _portName = string.Empty;
        }

        public SerialCommunicationHandler(string comPort)
        {
            _available = new List<string>();
            Log.WriteLine($"COMMFACTORY: Creating Serial handler on {comPort} ...");
            // Format: "Serial: /dev/ttyUSB1@19200" or "Serial: COM3@19200"
            var result = Regex.Match(comPort, @"Serial:\s*([^@]+)@?(\d+)?");
            if (result.Success)
            {
                _portName = result.Groups[1].Value.Trim();
                _port = new SerialPort(_portName);
                int rate = 19200;
                if (!string.IsNullOrEmpty(result.Groups[2].Value))
                    int.TryParse(result.Groups[2].Value, out rate);
                _port.BaudRate = rate;
                _port.DtrEnable = false;
                _port.ReadTimeout = 2000;
                _port.WriteTimeout = 2000;
            }
            else
            {
                _portName = string.Empty;
            }
        }

        public override string Name => "Serial Port";
        public override bool Connected => _port?.IsOpen ?? false;

        protected override void RunJob(Job job)
        {
            CommandResponse? response = null;
            if (_logJobs) Log.WriteLine("SERIAL: {0:0000}: [{1}] Processing Job", job.Number, job.Command);
            if (Connected && _port != null)
            {
                _port.DiscardInBuffer();
                try
                {
                    _port.Write(job.Command);
                }
                catch (Exception ex)
                {
                    job.OnFulFilled(new CommandResponse(string.Empty, false, $"Unable to write to {_portName}. " + ex.Message));
                    return;
                }

                try
                {
                    switch (job.ResponseType)
                    {
                        case ResponseType.NoResponse:
                            response = new CommandResponse(string.Empty, true);
                            break;
                        case ResponseType.DigitResponse:
                            response = new CommandResponse(new string((char)_port.ReadChar(), 1), true);
                            break;
                        case ResponseType.FullResponse:
                            response = new CommandResponse(_port.ReadTo("#"), true);
                            break;
                        case ResponseType.DoubleFullResponse:
                            var r1 = _port.ReadTo("#");
                            _port.ReadTo("#");
                            response = new CommandResponse(r1, true);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    response = new CommandResponse(string.Empty, false, $"Unable to read response to {job.Command} from {_portName}. {ex.Message}");
                }
            }
            else
            {
                response = new CommandResponse(string.Empty, false, $"Unable to open {_portName}");
            }

            job.OnFulFilled(response!);
            job.Succeeded = response!.Success;
        }

        public override bool Connect()
        {
            if (_port != null && !_port.IsOpen)
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
                }
                catch (Exception ex)
                {
                    Log.WriteLine("SERIAL: Failed to open the port. {0}", ex.Message);
                }
            }
            return _port?.IsOpen ?? false;
        }

        public override void Disconnect()
        {
            StopJobsProcessor();
            if (_port != null && _port.IsOpen)
            {
                try
                {
                    _port.Write(":Qq#");
                    Thread.Sleep(10);
                }
                catch (Exception ex)
                {
                    Log.WriteLine("SERIAL: Failed to send disconnect command (port may have been lost). {0}", ex.Message);
                }
                try
                {
                    _port.Close();
                }
                catch (Exception ex)
                {
                    Log.WriteLine("SERIAL: Failed to close port. {0}", ex.Message);
                }
                _port = null;
            }
        }

        public override void DiscoverDeviceInstances(Action<string> addDevice)
        {
            Log.WriteLine("SERIAL: Checking Serial ports....");
            _available.Clear();
            foreach (var port in SerialPort.GetPortNames())
            {
                Log.WriteLine("SERIAL: Found Serial port [{0}]", port);
                string entry = "Serial: " + port;
                if (!_available.Contains(entry))
                {
                    _available.Add(entry);
                    addDevice(entry);
                }
            }
        }

        public override bool IsDriverForDevice(string device) => device.StartsWith("Serial: ");

        public override ICommunicationHandler CreateHandler(string device) => new SerialCommunicationHandler(device);
    }
}
