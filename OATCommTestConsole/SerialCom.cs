using System;
using System.IO.Ports;
using System.Threading.Tasks;

namespace OATCommTestConsole
{
    public class SerialCommunicationHandler
    {
        private string _portName;
        private SerialPort _port;

        private int _readTimeout = 250;
        private int _writeTimeout = 250;
        private int _baudRate = 57600;

        public SerialCommunicationHandler(string comPort)
        {
            Console.WriteLine($"COMMFACTORY: Creating Serial handler on {comPort} at {_baudRate} baud...");
            Console.WriteLine($"COMMFACTORY: Read Timeout {_readTimeout}");
            Console.WriteLine($"COMMFACTORY: Write Timeout {_writeTimeout}");

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
                requestIndex++;
                try
                {
                    Console.WriteLine("[{0:0000}] SERIAL: [{1}] Sending command", requestIndex, command);
                    _port.Write(command);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[{0:0000}] SERIAL: [{1}] Failed to send command. {2}", requestIndex, command, ex.Message);
                    Console.ResetColor();
                    return new CommandResponse(string.Empty, false, $"Unable to write to {_portName}. " + ex.Message);
                }

                try
                {
                    switch (needsResponse)
                    {
                        case ResponseType.NoResponse:
                            {
                                Console.WriteLine("[{0:0000}] SERIAL: [{1}] No response needed for command", requestIndex, command);
                                return new CommandResponse(string.Empty, true);
                            }

                        case ResponseType.DigitResponse:
                            {
                                Console.WriteLine("[{0:0000}] SERIAL: [{1}] Expecting single digit response for command, waiting...", requestIndex, command);
                                string response = new string((char)_port.ReadChar(), 1);
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("[{0:0000}] SERIAL: [{1}] Received single digit response '{2}'", requestIndex, command, response);
                                Console.ResetColor();
                                return new CommandResponse(response, true);
                            }

                        case ResponseType.FullResponse:
                            {
                                Console.WriteLine("[{0:0000}] SERIAL: [{1}] Expecting #-delimited response for Command, waiting...", requestIndex, command);
                                string response = _port.ReadTo("#");
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("[{0:0000}] SERIAL: [{1}] Received response '{2}'", requestIndex, command, response);
                                Console.ResetColor();
                                return new CommandResponse(response, true);
                            }
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[{0:0000}] SERIAL: [{1}] Failed to receive response to command. {2}", requestIndex, command, ex.Message);
                    Console.ResetColor();
                    return new CommandResponse(string.Empty, false, $"Unable to read response to {command} from {_portName}. {ex.Message}");
                }

                return new CommandResponse(string.Empty, false, "Something weird going on...");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[{0:0000}] SERIAL: Failed to open port {1}", requestIndex, _portName);
                Console.ResetColor();
                return new CommandResponse(string.Empty, false, $"Unable to open {_portName}");
            }
        }

        private async Task<bool> EnsurePortIsOpen()
        {
            if (!_port.IsOpen)
            {
                try
                {
                    Console.WriteLine("SERIAL: Port {0} is not open, attempting to open...", _portName);
                    _port.Open();

                    Console.WriteLine("SERIAL: Checking if buffer contains data", _portName);
                    string preCheck = _port.ReadExisting();
                    if(!string.IsNullOrEmpty(preCheck))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("SERIAL: Possible problem, data in buffer. {0}", preCheck);
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("SERIAL: No exising serial data in buffer, all good...");
                        Console.ResetColor();
                    }
                    await Task.Delay(750); // Arduino resets on connection. Give it time to start up.
                    Console.WriteLine("SERIAL: Port is open, sending initial [:I#] command..");
                    _port.Write(":I#");
                    return true;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("SERIAL: Failed to open the port. {0}", ex.Message);
                    Console.ResetColor();
                    return false;
                }
            }
            Console.WriteLine("SERIAL: Port {0} is open, continuing...", _portName);
            return true;
        }

        public void Disconnect()
        {
            if (_port.IsOpen)
            {
                Console.WriteLine("SERIAL: Port is open, sending shutdown command [:Qq#]");
                _port.Write(":Qq#");
                Console.WriteLine("SERIAL: Closing port...");
                _port.Close();
                _port = null;
                Console.WriteLine("SERIAL: Disconnected...");
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