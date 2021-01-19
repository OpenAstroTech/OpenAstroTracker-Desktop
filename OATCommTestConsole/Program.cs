using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Management;
using System.Threading.Tasks;

namespace OATCommTestConsole
{
    class Program
    {
        private static SerialCommunicationHandler _commHandler;
        static int _readTimeout = 250;
        static int _writeTimeout = 250;
        static int _baudRate = 57600;

        static System.ConsoleColor _menuColor = ConsoleColor.DarkYellow;

        static void Main(string[] args)
        {
            MainAsync().Wait();
        }

        static async Task MainAsync()
        {
            while (true)
            {
                int userChoice;
                do
                {
                    Console.Clear();
                    Console.ForegroundColor = _menuColor;
                    Console.WriteLine("---------------------------\r");
                    Console.WriteLine("  OAT Communication Test\r");
                    Console.WriteLine("        Main Menu\r");
                    Console.WriteLine("---------------------------\r");
                    Console.WriteLine("Choose one of the following options:\r");

                    Console.WriteLine("[ 1 ] Settings");
                    Console.WriteLine("[ 2 ] Command Test");
                    Console.WriteLine("[ 3 ] Run Test/Get Info");
                    Console.WriteLine("[ 0 ] Quit application\n");
                    Console.ResetColor();

                } while (!int.TryParse(Console.ReadLine(), out userChoice) || userChoice < 0 || userChoice > 3);

                Console.Clear();
                switch (userChoice)
                {
                    case 1:
                        Settings();
                        break;
                    case 2:
                        var result = await Connect();
                        if(result)
                        {
                            await CustomCommand();
                        }
                        break;
                    case 3:
                        result = await Connect();
                        if (result)
                        {
                            await StartTest();
                        }
                        break;
                    case 0:
                        Environment.Exit(0);
                        break;
                    default:
                        Console.WriteLine("Try again!!");
                        break;
                }
            }
            
        }

        static async Task CustomCommand()
        {
            // Console.Clear();
            string userChoice = "";
            Console.ForegroundColor = _menuColor;
            Console.WriteLine("---------------------------\r");
            Console.WriteLine("  OAT Communication Test\r");
            Console.WriteLine("      Custom Command\r");
            Console.WriteLine("---------------------------\r");
            Console.WriteLine("[ 0 ] Return to Main Menu\n");
            Console.WriteLine("Enter MEADE Command, examples: \r");
            Console.WriteLine(":GX#,#> - for a full reply\r");
            Console.WriteLine(":SG+01#,n - single digit reply\r");
            Console.WriteLine(":Qn#, - when no reply\r");
            Console.ResetColor();

            while (true)
            {
                userChoice = Console.ReadLine();

                switch (userChoice)
                {
                    case "0":
                        _commHandler.Disconnect();
                        return;

                    default:
                        Console.WriteLine("Sending command: {0}", userChoice);
                        await SendCommand(userChoice);
                        break;
                }
            }
        }

        static void Settings()
        {
            while (true)
            {
                int userChoice;
                do
                {
                    Console.Clear();
                    Console.ForegroundColor = _menuColor;
                    Console.WriteLine("---------------------------\r");
                    Console.WriteLine("  OAT Communication Test\r");
                    Console.WriteLine("       Settings\r");
                    Console.WriteLine("---------------------------\r");
                    Console.WriteLine("Choose one of the following options:\r");

                    Console.WriteLine("[ 1 ] Read Timeout ({0})", _readTimeout);
                    Console.WriteLine("[ 2 ] Write Timeout ({0})", _writeTimeout);
                    Console.WriteLine("[ 3 ] Baud Rate ({0})", _baudRate);
                    Console.WriteLine("[ 0 ] Return\n");
                    Console.ResetColor();

                } while (!int.TryParse(Console.ReadLine(), out userChoice) || userChoice < 0 || userChoice > 3);

                switch (userChoice)
                {
                    case 1:
                        Console.WriteLine("Set Read Timeout({0}ms):", _readTimeout);
                        var newVal = Convert.ToInt32(Console.ReadLine());
                        _readTimeout = newVal;
                        break;
                    case 2:
                        Console.WriteLine("Set Write Timeout({0}ms):", _writeTimeout);
                        newVal = Convert.ToInt32(Console.ReadLine());
                        _writeTimeout = newVal;
                        break;
                    case 3:
                        Console.WriteLine("Set BaudRate({0}):", _baudRate);
                        newVal = Convert.ToInt32(Console.ReadLine());
                        _baudRate = newVal;
                        break;
                    case 0:
                        return;
                    default:
                        Console.WriteLine("Try again!!");
                        break;
                }
            }
            
        }

        static async Task<bool> Connect()
        {
            while (true)
            {
                // List available COM devices
                var devices = DiscoverDevices();

                int userChoice;
                do
                {
                    Console.Clear();
                    Console.ForegroundColor = _menuColor;
                    Console.WriteLine("---------------------------\r");
                    Console.WriteLine("  OAT Communication Test\r");
                    Console.WriteLine("         Connecct\r");
                    Console.WriteLine("---------------------------\r");
                    Console.WriteLine("Select Serial Port:\r");
                    
                    int cnt = 1;
                    foreach (var dev in devices)
                    {
                        Console.Write("[ {0} ] {1}\n", cnt, dev);
                        cnt++;
                    }
                    Console.WriteLine("[ 0 ] Return\n");
                    Console.ResetColor();

                } while (!int.TryParse(Console.ReadLine(), out userChoice) || userChoice < 0 || userChoice > devices.Count);

                if(userChoice == 0)
                {
                    return false;
                }
                else if(userChoice >= 0 && userChoice <= devices.Count )
                {
                    var connectResult = await CreateCommHandler(devices[userChoice-1]);
                    if(connectResult)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine("Try again!!");
                }
            }
        }


        static async Task<bool> StartTest()
        {
            Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
            keyValuePairs.Add("GVP#,#", "Product name");
            keyValuePairs.Add("GVN#,#", "Firmare version");
            keyValuePairs.Add("XGM#,#", "Mount configuration");
            keyValuePairs.Add("Gt#,#", "Site Latitude");
            keyValuePairs.Add("Gg#,#", "Site Longitude");
            keyValuePairs.Add("XGR#,#", "RA Steps");
            keyValuePairs.Add("XGD#,#", "DEC Steps");
            keyValuePairs.Add("XGS#,#", "Tracking speed adjustment");
            keyValuePairs.Add("XGT#,#", "Tracking speed");
            keyValuePairs.Add("XGH#,#", "HA");
            keyValuePairs.Add("XGL#,#", "LST");
            keyValuePairs.Add("XGN#,#", "Network settings");

            List<CommandResponse> replys = new List<CommandResponse>();
            foreach (var cmd in keyValuePairs)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("----- {0} -----\r", cmd.Value);
                Console.ResetColor();
                var result = await SendCommand(cmd.Key);
                if (!result.Success)
                {
                    Console.WriteLine("Press any key to return...");
                    Console.ReadKey();
                    return false;
                }
                replys.Add(result);
            }
            
            // Disconnect
            _commHandler.Disconnect();

            // Print summery
            int cnt = 0;
            Console.WriteLine("--------------------------------------- SUMMERY -----------------------------------------------------------\r");
            foreach (var cmd in keyValuePairs)
            {
                Console.WriteLine("| {0} | {1} |\r", cmd.Value.PadLeft(30), replys[cnt].Data.PadRight(70));
                cnt++;
            }
            Console.WriteLine("-----------------------------------------------------------------------------------------------------------\r");

            Console.WriteLine("Press any key to return...");
            Console.ReadKey();

            return true;
        }
        
        static async Task<bool> CreateCommHandler(string device)
        {
            _commHandler = ConnectToDevice(device);
            _commHandler.ReadTimeout = _readTimeout;
            _commHandler.WriteTimeout = _writeTimeout;
            _commHandler.BaudRate = _baudRate;

            await SendCommand(":I#,");

            return true;
        }

        static List<string> DiscoverDevices()
        {
            Console.WriteLine("COMMFACTORY: Device Discovery initiated.");
            Console.WriteLine("COMMFACTORY: Checking Serial ports....");

            List<string> _available = new List<string>();
            foreach (var port in SerialPort.GetPortNames())
            {
                Console.WriteLine("COMMFACTORY: Found Serial port [{0}]", port);
                _available.Add("Serial : " + port);
            }
            return _available;
        }

        static SerialCommunicationHandler ConnectToDevice(string device)
        {
            Console.WriteLine($"COMMFACTORY: Attempting to connect to device {device}...");
            if (device.StartsWith("Serial : "))
            {
                string comPort = device.Substring("Serial : ".Length);
                return new SerialCommunicationHandler(comPort);
            }
            return null;
        }

        private static async Task<CommandResponse> SendCommand(string cmd)
        {
            if (!cmd.StartsWith(":"))
            {
                cmd = $":{cmd}";
            }

            if (cmd.EndsWith("#,#"))
            {
                return await _commHandler.SendCommand(cmd.Substring(0, cmd.Length - 2));
            }
            else if (cmd.EndsWith("#,n"))
            {
                return await _commHandler.SendCommandConfirm(cmd.Substring(0, cmd.Length - 2));
            }

            if (!cmd.EndsWith("#"))
            {
                cmd += "#";
            }
            return await _commHandler.SendBlind(cmd);
        }

        /*
        static void PortInfo()
        {
            ManagementObjectCollection mbsList = null;
            ManagementObjectSearcher mbs = new ManagementObjectSearcher("Select * From Win32_SerialPort");
            mbsList = mbs.Get();

            foreach (ManagementObject mo in mbsList)
            {
                Console.WriteLine("Description:{0}", mo["Description"].ToString());

            }
        }
        */
    }
}
