using System;
using System.IO.Ports;
using System.Threading.Tasks;

namespace OATCommTestConsole
{
    public class SerialCommunicationHandler
    {
        private string _portName;
        private SerialPort _port;

        private int _readTimeout = 1000;
        private int _writeTimeout = 250;
        private int _baudRate = 57600;

        public SerialCommunicationHandler(string comPort)
        {
            
            ConsoleOutput.Info($"COMMFACTORY: Creating Serial handler on {comPort} at {_baudRate} baud...");
            ConsoleOutput.Info($"COMMFACTORY: Read Timeout {_readTimeout}");
            ConsoleOutput.Info($"COMMFACTORY: Write Timeout {_writeTimeout}");

            _portName = comPort;
            _port = new SerialPort(comPort);
            _port.BaudRate = _baudRate;
            _port.DtrEnable = false;
            _port.ReadTimeout = _readTimeout;
            _port.WriteTimeout = _writeTimeout;
        }

        public bool Connected => _port.IsOpen;

        public async Task<CommandResponse> SendBlind(string command)
        {
            return await SendCommand(command, ResponseType.NoResponse);
        }

        public async Task<CommandResponse> SendCommand(string command)
        {
            return await SendCommand(command, ResponseType.FullResponse);
        }

        public async Task<CommandResponse> SendCommandConfirm(string command)
        {
            return await SendCommand(command, ResponseType.DigitResponse);
        }

        long requestIndex = 1;

        private async Task<CommandResponse> SendCommand(string command, ResponseType needsResponse)
        {
            if (await EnsurePortIsOpen())
            {
                _port.DiscardInBuffer();
                _port.DiscardOutBuffer();

                requestIndex++;
                try
                {
                    ConsoleOutput.Info(string.Format("[{0:0000}] SERIAL: [{1}] Sending command", requestIndex, command));
                    _port.Write(command);
                }
                catch (Exception ex)
                {
                    ConsoleOutput.Error(string.Format("[{0:0000}] SERIAL: [{1}] Failed to send command. {2}", requestIndex, command, ex.Message));
                    return new CommandResponse(string.Empty, false, $"Unable to write to {_portName}. " + ex.Message);
                }

                try
                {
                    switch (needsResponse)
                    {
                        case ResponseType.NoResponse:
                            {
                                ConsoleOutput.Info(string.Format("[{0:0000}] SERIAL: [{1}] No response needed for command", requestIndex, command));
                                return new CommandResponse(string.Empty, true);
                            }

                        case ResponseType.DigitResponse:
                            {
                                ConsoleOutput.Info(string.Format("[{0:0000}] SERIAL: [{1}] Expecting single digit response for command, waiting...", requestIndex, command));
                                string response = new string((char)_port.ReadChar(), 1);
                                ConsoleOutput.Success(string.Format("[{0:0000}] SERIAL: [{1}] Received single digit response '{2}'", requestIndex, command, response));
                                return new CommandResponse(response, true);
                            }

                        case ResponseType.FullResponse:
                            {
                                ConsoleOutput.Info(string.Format("[{0:0000}] SERIAL: [{1}] Expecting #-delimited response for Command, waiting...", requestIndex, command));
                                string response = _port.ReadTo("#");
                                ConsoleOutput.Success(string.Format("[{0:0000}] SERIAL: [{1}] Received response '{2}'", requestIndex, command, response));
                                return new CommandResponse(response, true);
                            }
                    }
                }
                catch (Exception ex)
                {
                    
                    ConsoleOutput.Error(string.Format("[{0:0000}] SERIAL: [{1}] Failed to receive response to command. {2}", requestIndex, command, ex.Message));
                    return new CommandResponse(string.Empty, false, $"Unable to read response to {command} from {_portName}. {ex.Message}");
                }

                return new CommandResponse(string.Empty, false, "Something weird going on...");
            }
            else
            {
                ConsoleOutput.Error(string.Format("[{0:0000}] SERIAL: Failed to open port {1}", requestIndex, _portName));
                return new CommandResponse(string.Empty, false, $"Unable to open {_portName}");
            }
        }

        private async Task<bool> EnsurePortIsOpen()
        {
            if (!_port.IsOpen)
            {
                try
                {
                    ConsoleOutput.Info(string.Format("SERIAL: Port {0} is not open, attempting to open...", _portName));
                    _port.Open();

                    ConsoleOutput.Info(string.Format("SERIAL: Checking if buffer contains data", _portName));
                    string preCheck = _port.ReadExisting();
                    if(!string.IsNullOrEmpty(preCheck))
                    {
                        ConsoleOutput.Error(string.Format("SERIAL: Possible problem, data already in buffer: {0}", preCheck));
                        ConsoleOutput.Warning("SERIAL: Flushing serial buffers...");
                        _port.DiscardInBuffer();
                        _port.DiscardOutBuffer();
                    }
                    else
                    {
                        ConsoleOutput.Success("SERIAL: No exising serial data in buffer, all good...");
                    }
                    await Task.Delay(750); // Arduino resets on connection. Give it time to start up.
                    ConsoleOutput.Info("SERIAL: Port is open, sending initial [:I#] command..");
                    _port.Write(":I#");

                    ConsoleOutput.Success("SERIAL: Connected!");
                    return true;
                }
                catch (Exception ex)
                {
                    ConsoleOutput.Error(string.Format("SERIAL: Failed to open the port. {0}", ex.Message));
                    return false;
                }
            }


            ConsoleOutput.Warning("SERIAL: Flushing serial buffers...");
            _port.DiscardInBuffer();
            _port.DiscardOutBuffer();
            Console.WriteLine("SERIAL: Port {0} is open, continuing...", _portName);
            return true;
        }

        public void Disconnect()
        {
            if (_port.IsOpen)
            {
                ConsoleOutput.Info("SERIAL: Port is open, sending shutdown command [:Qq#]");
                _port.Write(":Qq#");
                ConsoleOutput.Info("SERIAL: Closing port...");
                _port.Close();
                _port = null;
                ConsoleOutput.Success("SERIAL: Disconnected...");
            }
        }

        public int ReadTimeout
        {
            get { return _readTimeout; }
            set { _readTimeout = value; }
        }

        public int WriteTimeout
        {
            get { return _writeTimeout; }
            set { _writeTimeout = value; }
        }

        public int BaudRate
        {
            get { return _baudRate; }
            set { _baudRate = value; }
        }

        
    }
}