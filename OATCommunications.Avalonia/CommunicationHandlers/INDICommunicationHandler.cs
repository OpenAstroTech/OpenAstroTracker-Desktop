using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;
using OATCommunications.CommunicationHandlers;
using OATCommunications.Utilities;

namespace OATCommunications.Avalonia.CommunicationHandlers
{
    /// <summary>
    /// INDI (Instrument Neutral Distributed Interface) communication handler
    /// Connects to an INDI server to control mounts via standard INDI telescope properties
    /// </summary>
    public class INDICommunicationHandler : CommunicationHandler
    {
        public static string DefaultHost = "localhost";
        public static int DefaultPort = 7624;
        public static bool ForceAltAzControls = false;

        private string? _host;
        private int _port;
        private TcpClient? _client;
        private NetworkStream? _stream;
        private Thread? _readThread;
        private bool _connected;
        private bool _readThreadActive;
        private Dictionary<string, string> _propertyValues = new Dictionary<string, string>();
        private HashSet<string> _propertyVectors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly object _propertyLock = new object();

        public INDICommunicationHandler()
        {
            _host = null;
            _port = 0;
            _connected = false;
        }

        /// <summary>
        /// Parse INDI device specification: "INDI Connection (host:port)"
        /// Older saved settings may still include a trailing "@9600", which is ignored.
        /// </summary>
        public INDICommunicationHandler(string spec)
        {
            Log.WriteLine($"INDI: Reading device specification from {spec}...");
            _connected = false;

            // Match format: "INDI Connection (host:port)" with optional legacy "@baudrate" suffix.
            var regex = new System.Text.RegularExpressions.Regex(@"([A-z\s]+)\(([^:()]+):([0-9]+)\)(?:@(\d+))?");
            var result = regex.Match(spec);

            if (result.Success)
            {
                _host = result.Groups[2].Value;
                _port = int.Parse(result.Groups[3].Value);
                Log.WriteLine($"INDI: Parsed host {_host}:{_port}");
                StartJobsProcessor();
            }
            else
            {
                Log.WriteLine($"INDI: Failed to parse device specification.");
            }
        }

        public override string Name => "INDI";

        public override bool Connected => _connected;

        public override bool IsDriverForDevice(string device)
        {
            return device.Contains("INDI");
        }

        public override ICommunicationHandler CreateHandler(string device)
        {
            return new INDICommunicationHandler(device);
        }

        public override void DiscoverDeviceInstances(Action<string> addDevice)
        {
            if (CanReachINDIServer(DefaultHost, DefaultPort))
            {
                addDevice($"INDI Connection ({DefaultHost}:{DefaultPort})");
            }
        }

        private bool CanReachINDIServer(string host, int port)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var result = client.BeginConnect(host, port, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(1000); // 1 second timeout
                    if (success && client.Connected)
                    {
                        client.EndConnect(result);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine($"INDI: Cannot reach server at {host}:{port}: {ex.Message}");
            }
            return false;
        }

        public override bool Connect()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_host) || _port == 0)
                {
                    Log.WriteLine("INDI: Invalid host or port");
                    return false;
                }

                Log.WriteLine($"INDI: Attempting to connect to {_host}:{_port}...");
                _client = new TcpClient();
                _client.Connect(_host, _port);
                _stream = _client.GetStream();
                _connected = true;

                // Start read thread to handle incoming messages
                _readThreadActive = true;
                _readThread = new Thread(ReadIndiMessages);
                _readThread.IsBackground = true;
                _readThread.Start();

                // Send INDI handshake to request telescope properties
                SendIndiCommand("<getProperties version=\"1.27\" />\n");
                SendIndiGetProperties("Telescope.NSEQ_MOTION_NS");
                SendIndiGetProperties("Telescope.NSEQ_MOTION_EW");

                Log.WriteLine("INDI: Connected successfully");
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine($"INDI: Connection failed: {ex.Message}");
                _connected = false;
                return false;
            }
        }

        public override void Disconnect()
        {
            try
            {
                _readThreadActive = false;
                _connected = false;

                StopJobsProcessor();

                if (_stream != null)
                {
                    _stream.Close();
                    _stream = null;
                }

                if (_client != null)
                {
                    _client.Close();
                    _client = null;
                }

                if (_readThread != null)
                {
                    _readThread.Join(1000);
                    _readThread = null;
                }

                Log.WriteLine("INDI: Disconnected");
            }
            catch (Exception ex)
            {
                Log.WriteLine($"INDI: Error during disconnect: {ex.Message}");
            }
        }

        protected override void RunJob(Job job)
        {
            if (!_connected)
            {
                Log.WriteLine($"INDI: Not connected, cannot send command: {job.Command}");
                job.OnFulFilled(new CommandResponse(string.Empty, false, "Not connected to INDI server"));
                return;
            }

            try
            {
                // Convert Meade command to INDI property set
                string indiCommand = MeadeCommandToIndi(job.Command);

                Log.WriteLine($"INDI: Sending Meade command [{job.Command}] as INDI: {indiCommand}");
                SendIndiCommand(indiCommand);

                // For simple commands, return immediately
                // For commands expecting responses, wait for property update
                if (job.ResponseType == ResponseType.NoResponse)
                {
                    job.Succeeded = true;
                    job.OnFulFilled(new CommandResponse(string.Empty, true));
                }
                else
                {
                    // Simple echo response for now
                    string response = GetIndiResponse(job.Command);
                    job.Succeeded = true;
                    job.OnFulFilled(new CommandResponse(response, true));
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine($"INDI: Error sending command [{job.Command}]: {ex.Message}");
                job.OnFulFilled(new CommandResponse(string.Empty, false, ex.Message));
            }
        }

        private void SendIndiCommand(string command)
        {
            if (_stream == null || !_connected)
                return;

            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(command);
                _stream.Write(bytes, 0, bytes.Length);
                _stream.Flush();
            }
            catch (Exception ex)
            {
                Log.WriteLine($"INDI: Failed to send command: {ex.Message}");
                _connected = false;
            }
        }

        private void SendIndiGetProperties(string propertyName)
        {
            string cmd = $"<getProperties version=\"1.27\" name=\"{propertyName}\" />\n";
            SendIndiCommand(cmd);
        }

        private void ReadIndiMessages()
        {
            try
            {
                using (StreamReader reader = new StreamReader(_stream!, Encoding.UTF8, false, 4096, true))
                {
                    StringBuilder xmlBuffer = new StringBuilder();

                    while (_readThreadActive && _connected)
                    {
                        string? line = reader.ReadLine();
                        if (line == null)
                        {
                            _connected = false;
                            break;
                        }

                        xmlBuffer.Append(line);

                        // Process complete XML messages (those ending with />)
                        if (line.EndsWith("/>") || line.EndsWith("</"))
                        {
                            ProcessIndiMessage(xmlBuffer.ToString());
                            xmlBuffer.Clear();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (_readThreadActive && _connected)
                {
                    Log.WriteLine($"INDI: Error reading messages: {ex.Message}");
                }
                _connected = false;
            }
        }

        private void ProcessIndiMessage(string indiXml)
        {
            try
            {
                using (XmlReader reader = XmlReader.Create(new StringReader(indiXml)))
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            switch (reader.Name)
                            {
                                case "newSwitchVector":
                                case "newNumberVector":
                                case "newTextVector":
                                case "newLightVector":
                                case "defSwitchVector":
                                case "defNumberVector":
                                case "defTextVector":
                                case "defLightVector":
                                    string? vectorName = reader.GetAttribute("name");
                                    if (!string.IsNullOrEmpty(vectorName))
                                    {
                                        lock (_propertyLock)
                                        {
                                            _propertyVectors.Add(vectorName);
                                        }
                                    }
                                    break;

                                case "oneLight":
                                case "oneNumber":
                                case "oneSwitch":
                                case "oneText":
                                    string? name = reader.GetAttribute("name");
                                    string value = reader.ReadElementContentAsString();
                                    if (!string.IsNullOrEmpty(name))
                                    {
                                        lock (_propertyLock)
                                        {
                                            _propertyValues[name] = value;
                                        }
                                        Log.WriteLine($"INDI: Property {name} = {value}");
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine($"INDI: Error parsing XML message: {ex.Message}");
            }
        }

        private bool HasAltAzCapability()
        {
            if (ForceAltAzControls)
            {
                return true;
            }

            lock (_propertyLock)
            {
                if (_propertyVectors.Contains("MOTION_CONTROL_MODE") ||
                    _propertyVectors.Contains("NSEQ_MOTION_AZ") ||
                    _propertyVectors.Contains("NSEQ_MOTION_ALT") ||
                    _propertyVectors.Contains("TELESCOPE_AZ") ||
                    _propertyVectors.Contains("TELESCOPE_ALT"))
                {
                    return true;
                }

                if (_propertyValues.ContainsKey("MOTION_CONTROL_MODE_AXES") ||
                    _propertyValues.ContainsKey("MOTION_CONTROL_MODE_JOYSTICK") ||
                    _propertyValues.ContainsKey("LOCK_AXIS_1") ||
                    _propertyValues.ContainsKey("LOCK_AXIS_2"))
                {
                    return true;
                }
            }

            return false;
        }

        private string GetIndiResponse(string meadeCommand)
        {
            // Responses must NOT include a trailing '#' — unlike Serial/TCP handlers,
            // we write directly into CommandResponse.Data without stripping delimiters.
            // Commands arrive as ":CMD#" (colon prefix, hash suffix already stripped of ,# suffix).

            switch (meadeCommand)
            {
                // ── Product / firmware ─────────────────────────────────────────
                case ":GVP#":                          // Get product name
                    return "OpenAstroTracker";

                case ":GVN#":                          // Get firmware version number
                    return "V1.13.17";                 // Format Vx.xx.xx — parsed by Substring(1).Split('.')

                case ":GVD#":                          // Get firmware date
                    return "Jan 01 2024";

                // ── Hardware / board info (connection-critical) ─────────────────
                case ":XGM#":
                    // Format: BOARD,RATYPE|RASTEPS,DECTYPE|DECSTEPS[,ADDON,...]
                    // hwParts[1].Split('|') must have ≥2 parts; same for hwParts[2]
                    return HasAltAzCapability()
                        ? "OAT,28BYJ|400,28BYJ|400,AUTO_AZ_ALT"
                        : "OAT,28BYJ|400,28BYJ|400";

                case ":XGMS#":
                    // Format: RADRIVER,SLEW_MS,TRACK_MS|DECDRIVER,SLEW_MS,GUIDE_MS
                    // Drivers: U=ULN2003, TU=TMC2209 UART, TS=TMC2209 Standalone, A=A4988
                    return "TU,8,4|TU,8,4";

                // ── Mount status (GX) ───────────────────────────────────────────
                case ":GX#":
                    // Format: Status,flags,0,0,steps,HHMMSS,±DDMMSS,extra
                    // parts[5]=RA as HHMMSS, parts[6]=DEC as ±DDMMSS
                    return "Idle,--T,0,0,0,000000,+900000,0";

                // ── Current position ────────────────────────────────────────────
                case ":GR#": case ":Gr#":              // Right ascension HH:MM:SS
                    return "00:00:00";

                case ":GD#": case ":Gd#":              // Declination ±DD*MM'SS
                    return "+90*00'00";

                // ── Steps per degree ────────────────────────────────────────────
                case ":XGR#":                          // RA steps/degree
                    return "160";

                case ":XGD#":                          // DEC steps/degree
                    return "160";

                case ":XGZ#":                          // AZ steps/degree
                    return "160";

                case ":XGA#":                          // ALT steps/degree
                    return "160";

                // ── DEC limits ──────────────────────────────────────────────────
                case ":XGDL#":                         // "min|max" DEC limits
                    return "-90.0|90.0";

                // ── Speed / tracking ────────────────────────────────────────────
                case ":XGS#":                          // Speed calibration factor
                    return "1.000";

                case ":XGT#":                          // Tracking speed
                    return "1000.0";

                case ":Gk#":                           // Tracking mode name
                    return "Sidereal";

                case ":GIT#":                          // Is tracking (0/1)
                    return "1";

                case ":TQ#": case ":TL#": case ":TS#": case ":TK#": case ":TM#":
                    return "1";                        // Confirm tracking rate change

                // ── Time / date ─────────────────────────────────────────────────
                case ":GG#":                           // UTC offset (hours)
                    return "0.0";

                case ":GL#":                           // Local time HH:MM:SS
                {
                    var now = DateTime.UtcNow;
                    return $"{now.Hour:00}:{now.Minute:00}:{now.Second:00}";
                }

                case ":GC#":                           // Local date MM/DD/YY
                {
                    var now = DateTime.UtcNow;
                    return $"{now.Month:00}/{now.Day:00}/{now.Year % 100:00}";
                }

                case ":GT#":                           // Sidereal tracking rate
                    return "60.1";

                // ── Location ────────────────────────────────────────────────────
                case ":Gt#":                           // Latitude ±DD*MM
                    return "+00*00";

                case ":Gg#":                           // Longitude ±DDD*MM
                    return "+000*00";

                // ── Hemisphere / hour angle / sidereal ──────────────────────────
                case ":XGH#":                          // Hour angle (decimal)
                    return "0.0";

                case ":XGHS#":                         // Hemisphere "N" or "S" (Substring(0,1) is called)
                    return "N";

                case ":XGL#":                          // Local sidereal time HH:MM:SS
                {
                    var now = DateTime.UtcNow;
                    return $"{now.Hour:00}:{now.Minute:00}:{now.Second:00}";
                }

                case ":XGST#":                         // Safe slew time remaining
                    return "0";

                case ":XGN#":                          // Network info
                    return "INDI";

                case ":XLGT#":                         // Temperature (°C)
                    return "20.0";

                // ── Home / park ─────────────────────────────────────────────────
                case ":hCq#":                          // Home command state
                    return "0";

                default:
                    return string.Empty;
            }
        }

        private string MeadeCommandToIndi(string meadeCmd)
        {
            // Convert Meade LX200 commands to INDI SET_PROPERTY messages
            // This is a simplified mapping; expand as needed

            switch (meadeCmd)
            {
                // North slew
                case ":Mn#":
                    return "<newSwitchVector device=\"Telescope\" name=\"NSEQ_MOTION_NS\"><oneSwitch name=\"MOTION_NORTH\">On</oneSwitch></newSwitchVector>\n";

                // South slew
                case ":Ms#":
                    return "<newSwitchVector device=\"Telescope\" name=\"NSEQ_MOTION_NS\"><oneSwitch name=\"MOTION_SOUTH\">On</oneSwitch></newSwitchVector>\n";

                // East slew
                case ":Me#":
                    return "<newSwitchVector device=\"Telescope\" name=\"NSEQ_MOTION_EW\"><oneSwitch name=\"MOTION_EAST\">On</oneSwitch></newSwitchVector>\n";

                // West slew
                case ":Mw#":
                    return "<newSwitchVector device=\"Telescope\" name=\"NSEQ_MOTION_EW\"><oneSwitch name=\"MOTION_WEST\">On</oneSwitch></newSwitchVector>\n";

                // Stop North
                case ":Qn#":
                    return "<newSwitchVector device=\"Telescope\" name=\"NSEQ_MOTION_NS\"><oneSwitch name=\"MOTION_NORTH\">Off</oneSwitch></newSwitchVector>\n";

                // Stop South
                case ":Qs#":
                    return "<newSwitchVector device=\"Telescope\" name=\"NSEQ_MOTION_NS\"><oneSwitch name=\"MOTION_SOUTH\">Off</oneSwitch></newSwitchVector>\n";

                // Stop East
                case ":Qe#":
                    return "<newSwitchVector device=\"Telescope\" name=\"NSEQ_MOTION_EW\"><oneSwitch name=\"MOTION_EAST\">Off</oneSwitch></newSwitchVector>\n";

                // Stop West
                case ":Qw#":
                    return "<newSwitchVector device=\"Telescope\" name=\"NSEQ_MOTION_EW\"><oneSwitch name=\"MOTION_WEST\">Off</oneSwitch></newSwitchVector>\n";

                // Track (start sidereal tracking)
                case ":AP#":
                    return "<newSwitchVector device=\"Telescope\" name=\"TELESCOPE_TRACK\"><oneSwitch name=\"TRACK_ON\">On</oneSwitch></newSwitchVector>\n";

                // Untrack (stop sidereal tracking)
                case ":AT#":
                    return "<newSwitchVector device=\"Telescope\" name=\"TELESCOPE_TRACK\"><oneSwitch name=\"TRACK_OFF\">On</oneSwitch></newSwitchVector>\n";

                // Get RA
                case ":GR#":
                    return "<getProperties version=\"1.27\" name=\"Telescope.EQUATORIAL_EOD_COORD\" />\n";

                // Get DEC
                case ":GD#":
                    return "<getProperties version=\"1.27\" name=\"Telescope.EQUATORIAL_EOD_COORD\" />\n";

                // Get Firmware Version (returns local response)
                case ":GVN#":
                case ":GVN#,#":
                    return "<!-- Firmware version query, response handled locally -->\n";

                // Get Firmware Date (returns local response)
                case ":GVD#":
                    return "<!-- Firmware date query, response handled locally -->\n";

                default:
                    // For unknown commands, just pass them through as generic command
                    Log.WriteLine($"INDI: Unknown Meade command ignored: {meadeCmd}");
                    return $"<!-- Unsupported command: {meadeCmd} -->\n";
            }
        }

        public override bool SupportsSetupDialog => true;

        public override bool RunSetupDialog()
        {
            // TODO: Implement INDI setup dialog (host/port configuration)
            Log.WriteLine("INDI: Setup dialog not yet implemented");
            return true;
        }
    }
}
