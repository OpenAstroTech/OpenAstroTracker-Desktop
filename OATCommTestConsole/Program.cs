using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Management;
using System.Globalization;

namespace OATCommTestConsole
{
    class Program
    {
        private static SerialCommunicationHandler _commHandler;
        static int _readTimeout = 450;
        static int _writeTimeout = 250;
        static int _baudRate = 57600;

        static CultureInfo _oatCulture = new CultureInfo("en-US");

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
                    ConsoleOutput.Logo();
                    Console.ForegroundColor = ConsoleOutput.menuColor;
                    Console.WriteLine("---------------------------\r");
                    Console.WriteLine("  OAT Communication Test\r");
                    Console.WriteLine("        Main Menu\r");
                    Console.WriteLine("---------------------------\r");
                    Console.WriteLine("Choose one of the following options:\r");

                    Console.WriteLine("[ 1 ] COM Settings");
                    Console.WriteLine("[ 2 ] Manual Command Test");
                    Console.WriteLine("[ 3 ] Test Gyro");
                    Console.WriteLine("[ 4 ] Test GPS");
                    Console.WriteLine("[ 5 ] Run Test/Compile Info");
                    Console.WriteLine("[ 0 ] Quit application\n");
                    Console.ResetColor();

                } while (!int.TryParse(Console.ReadLine(), out userChoice) || userChoice < 0 || userChoice > 5);

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
                            await TestGyro();
                        }
                        break;
                    case 4:
                        result = await Connect();
                        if (result)
                        {
                            await TestGPS();
                        }
                        break;
                    case 5:
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

        static void Settings()
        {
            while (true)
            {
                int userChoice;
                do
                {
                    Console.Clear();
                    Console.ForegroundColor = ConsoleOutput.menuColor;
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
                    // ListComDetails();
                    ConsoleOutput.PreTestInfo();
                    Console.ForegroundColor = ConsoleOutput.menuColor;
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

        static async Task CustomCommand()
        {
            // Console.Clear();
            string userChoice = "";
            Console.ForegroundColor = ConsoleOutput.menuColor;
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
                        _commHandler = null;
                        return;

                    default:
                        Console.WriteLine("Sending command: {0}", userChoice);
                        await SendCommand(userChoice);
                        break;
                }
            }
        }

        static async Task<bool> TestGyro()
        {
            Console.WriteLine("Press ESC to stop");
            do
            {
                var result = await SendCommand(":XL1#,#");

                while (!Console.KeyAvailable)
                {
                    Console.Clear();
                    Console.WriteLine("********* GYRO TEST **************\r");
                    await SendCommand(":XLGC#,#");
                    Console.WriteLine("ESC - Exit\n");
                    await Task.Delay(250);
                }
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);

            await SendCommand(":XL0#,#");
            _commHandler.Disconnect();
            _commHandler = null;

            return true;
        }

        static async Task<bool> TestGPS()
        {   
            Console.Clear();
            Console.WriteLine("********* GPS TEST ***************\r");
            Console.WriteLine("* Battery can need to be charged *\r");
            Console.WriteLine("* first time GPS is used ~30min  *\r");
            Console.WriteLine("* 0 = No satelites found         *\r");
            Console.WriteLine("* 1 = Satelites found            *\r");
            Console.WriteLine("**********************************\r");
            Console.WriteLine("Press ESC to stop");
            do
            {
                while (!Console.KeyAvailable)
                {
                    var result = await SendCommand(":gT100#,n");
                    if (result.Data == "1")
                    {
                        _commHandler.ReadTimeout = 550;
                        result = await SendCommand(":Gt#,#");
                        var latitudeArray = result.Data.Split('*');
                        float latitude = float.Parse(latitudeArray[0], _oatCulture) + (float.Parse(latitudeArray[1], _oatCulture) / 60.0f);
                        await Task.Delay(250);
                        result = await SendCommand(":Gg#,#");
                        var longitudeArray = result.Data.Split('*');
                        float longitude = float.Parse(longitudeArray[0], _oatCulture) + (float.Parse(longitudeArray[1], _oatCulture) / 60.0f);
                        if (longitude > 180) longitude -= 360;
                        await Task.Delay(250);

                        ConsoleOutput.Error(string.Format("Lat: {0}", latitude));
                        ConsoleOutput.Error(string.Format("Lon: {0}", longitude));
                        _commHandler.ReadTimeout = 250;
                    }
                    
                    Console.WriteLine("ESC - Exit\n");
                    await Task.Delay(1000);
                }
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);


            _commHandler.Disconnect();
            _commHandler = null;

            Console.ReadKey();
            return true;
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
            _commHandler = null;

            // Print summery
            int cnt = 0;
            ConsoleOutput.Info("--------------------------------------- SUMMERY -----------------------------------------------------------\r");
            foreach (var cmd in keyValuePairs)
            {
                ConsoleOutput.Info(string.Format("| {0} | {1} |\r", cmd.Value.PadLeft(30), replys[cnt].Data.PadRight(70)));
                cnt++;
            }
            ConsoleOutput.Info("-----------------------------------------------------------------------------------------------------------\r");

            ConsoleOutput.Info("Press any key to return...");
            Console.ReadKey();

            return true;
        }
        
        static async Task<bool> CreateCommHandler(string device)
        {
            var tempCon = ConnectToDevice(device);
            if (tempCon == null)
                return true;
            
            _commHandler = tempCon;
            _commHandler.ReadTimeout = _readTimeout;
            _commHandler.WriteTimeout = _writeTimeout;
            _commHandler.BaudRate = _baudRate;

            await SendCommand(":I#,");

            return true;
        }

        static List<string> DiscoverDevices()
        {
            ConsoleOutput.Info("COMMFACTORY: Device Discovery initiated.");
            ConsoleOutput.Info("COMMFACTORY: Checking Serial ports....");

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
            if(_commHandler != null && _commHandler.Connected)
            {
                ConsoleOutput.Warning($"COMMFACTORY: Already connected to {device}...");
                return null;
            }

            ConsoleOutput.Info($"COMMFACTORY: Attempting to connect to device {device}...");
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

        private static void ListComDetails()
        {
            using (ManagementClass i_Entity = new ManagementClass("Win32_PnPEntity"))
            {
                foreach (ManagementObject i_Inst in i_Entity.GetInstances())
                {
                    Object o_Guid = i_Inst.GetPropertyValue("ClassGuid");
                    if (o_Guid == null || o_Guid.ToString().ToUpper() != "{4D36E978-E325-11CE-BFC1-08002BE10318}")
                        continue; // Skip all devices except device class "PORTS"

                    String s_Caption = i_Inst.GetPropertyValue("Caption").ToString();
                    String s_Manufact = i_Inst.GetPropertyValue("Manufacturer").ToString();
                    String s_DeviceID = i_Inst.GetPropertyValue("PnpDeviceID").ToString();
                    String s_RegPath = "HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\Enum\\" + s_DeviceID + "\\Device Parameters";
                    String s_PortName = Microsoft.Win32.Registry.GetValue(s_RegPath, "PortName", "").ToString();

                    int s32_Pos = s_Caption.IndexOf(" (COM");
                    if (s32_Pos > 0) // remove COM port from description
                        s_Caption = s_Caption.Substring(0, s32_Pos);

                    ConsoleOutput.Success("Port Name:    " + s_PortName);
                    ConsoleOutput.Success("Description:  " + s_Caption);
                    ConsoleOutput.Success("Manufacturer: " + s_Manufact);
                    ConsoleOutput.Success("Device ID:    " + s_DeviceID);
                    ConsoleOutput.Success("-----------------------------------");
                }
            }
        }

        private static void CheckHardware(string hardware)
        {

        }

    }

}
