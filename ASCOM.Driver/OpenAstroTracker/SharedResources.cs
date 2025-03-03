//
// ================
// Shared Resources
// ================
//
// This class is a container for all shared resources that may be needed
// by the drivers served by the Local Server. 
//
// NOTES:
//
//	* ALL DECLARATIONS MUST BE STATIC HERE!! INSTANCES OF THIS CLASS MUST NEVER BE CREATED!
//
// Written by:	Bob Denny	29-May-2007
// Modified by Chris Rowland and Peter Simpson to hamdle multiple hardware devices March 2011
//

using System;
using System.Windows.Forms;
using System.Threading;
using ASCOM.Utilities;


namespace ASCOM.OpenAstroTracker
{
	/// <summary>
	/// The resources shared by all drivers and devices, in this example it's a serial port with a shared SendMessage method
	/// an idea for locking the message and handling connecting is given.
	/// In reality extensive changes will probably be needed.
	/// Multiple drivers means that several applications connect to the same hardware device, aka a hub.
	/// Multiple devices means that there are more than one instance of the hardware, such as two focusers.
	/// In this case there needs to be multiple instances of the hardware connector, each with it's own connection count.
	/// </summary>
	public static class SharedResources
	{
		[Flags]
		public enum LoggingFlags
		{
			None = 0,
			Serial = 1,
			Server = 2,
			Scope = 4,
			Focuser = 8,

			Setup = 128,
		}

		// object used for locking to prevent multiple drivers accessing common code at the same time
		private static readonly object lockObject = new object();
		private static TraceLogger traceLogger;
		public static string driverID = "ASCOM.OpenAstroTracker.Telescope";
		private static long messageNumber = 1;
		private static LoggingFlags currentLogFlags = LoggingFlags.Scope | LoggingFlags.Focuser;

		private static string comPortProfileName = "COM Port";
		private static string baudRateProfileName = "Baud Rate";
		private static string traceStateProfileName = "Trace Level";
		private static string traceFlagsProfileName = "Trace Flags";
		private static string latitudeProfileName = "Latitude";
		private static string longitudeProfileName = "Longitude";
		private static string elevationProfileName = "Elevation";

		private static string comPortDefault = "COM5";
		private static string baudRateDefault = "19200";
		private static string traceStateDefault = "True";
		private static string traceFlagsDefault = "Scope, Focuser";
		private static double latitudeDefault = 30;
		private static double longitudeDefault = -97;
		private static double elevationDefault = 1;
		private static Mutex _commandMutex;

		static SharedResources()
		{
			EnsureLogger();
		}

		//
		// Public access to shared resources
		//
		public static Mutex OATCommandMutex
		{
			get
			{
				if (_commandMutex == null)
				{
					_commandMutex = new Mutex(false, "CommMutex");
				}
				return _commandMutex;
			}
		}

		private static void EnsureLogger()
		{
			lock (lockObject)
			{
				if (traceLogger == null)
				{
					traceLogger = new TraceLogger("", "OpenAstroTracker.LocalServer");
					traceLogger.Enabled = true;
				}
			}
		}

		//public static TraceLogger tl
		//{
		//	get
		//	{
		//		if (traceLogger == null)
		//		{
		//			traceLogger = new TraceLogger("", "OpenAstroTracker.LocalServer");
		//			traceLogger.Enabled = true;
		//		}

		//		return traceLogger;
		//	}
		//}

		public static ProfileData ReadProfile()
		{
			using (Profile driverProfile = new Profile())
			{
				driverProfile.DeviceType = "Telescope";
				return new ProfileData
				{
					TraceState = Convert.ToBoolean(driverProfile.GetValue(driverID, traceStateProfileName, String.Empty, traceStateDefault)),
					TraceFlags = (LoggingFlags)Enum.Parse(typeof(LoggingFlags), driverProfile.GetValue(driverID, traceFlagsProfileName, String.Empty, traceFlagsDefault)),
					ComPort = driverProfile.GetValue(driverID, comPortProfileName, string.Empty, comPortDefault),
					BaudRate = long.Parse(driverProfile.GetValue(driverID, baudRateProfileName, string.Empty, baudRateDefault)),
					Latitude = Convert.ToDouble(driverProfile.GetValue(driverID, latitudeProfileName, string.Empty, latitudeDefault.ToString())),
					Longitude = Convert.ToDouble(driverProfile.GetValue(driverID, longitudeProfileName, string.Empty, longitudeDefault.ToString())),
					Elevation = Convert.ToDouble(driverProfile.GetValue(driverID, elevationProfileName, string.Empty, elevationDefault.ToString()))
				};
			}
		}

		public static void WriteProfile(ProfileData profile)
		{
			using (Profile driverProfile = new Profile())
			{
				driverProfile.DeviceType = "Telescope";
				driverProfile.WriteValue(driverID, traceStateProfileName, profile.TraceState.ToString());
				driverProfile.WriteValue(driverID, traceFlagsProfileName, profile.TraceFlags.ToString());
				driverProfile.WriteValue(driverID, comPortProfileName, profile.ComPort?.ToString() ?? "");
				driverProfile.WriteValue(driverID, baudRateProfileName, profile.BaudRate.ToString());
				driverProfile.WriteValue(driverID, latitudeProfileName, profile.Latitude.ToString());
				driverProfile.WriteValue(driverID, longitudeProfileName, profile.Longitude.ToString());
				driverProfile.WriteValue(driverID, elevationProfileName, profile.Elevation.ToString());
				currentLogFlags = profile.TraceFlags;
			}
		}

		#region single serial port connector

		//
		// this region shows a way that a single serial port could be connected to by multiple 
		// drivers.
		//
		// Connected is used to handle the connections to the port.
		//
		// SendMessage is a way that messages could be sent to the hardware without
		// conflicts between different drivers.
		//
		// All this is for a single connection, multiple connections would need multiple ports
		// and a way to handle connecting and disconnection from them - see the
		// multi driver handling section for ideas.
		//

		/// <summary>
		/// Shared serial port
		/// </summary>
		public static Serial SharedSerial { get; } = new ASCOM.Utilities.Serial();

		/// <summary>
		/// number of connections to the shared serial port
		/// </summary>
		public static int Connections { get; set; } = 0;

		/// <summary>
		/// The possible replies that a Meade command can produce
		/// </summary>
		enum ExpectedAnswer
		{
			None,
			Digit,
			HashTerminated,
			DoubleHashTerminated,
		}

		/// <summary>
		/// A shared SendMessage method, the lock prevents different drivers tripping over one another.
		/// The message passed should end in one of these endings:
		///	 #   - the command does not expect a reply
		///	 #,n - the command expect a single numerical digit (always 0 or 1)
		///	 #,# - the command expects a string reply that ends in a #
		///	 It is very important to send the right variation since otherwise the protocol will become confused.
		/// </summary>
		public static string SendMessage(string message)
		{
			string retVal = string.Empty;
			long messageNr = Interlocked.Increment(ref messageNumber);
			LogMessage(LoggingFlags.Serial, $"SendMessage Nr{messageNr,0:0000} > ({message}) - awaiting lock");
			lock (lockObject)
			{
				LogMessage(LoggingFlags.Serial, $"SendMessage Nr{messageNr,0:0000} - acquired lock");

				ExpectedAnswer expect = ExpectedAnswer.None;

				if (message.EndsWith("#,#"))
				{
					message = message.Substring(0, message.Length - 2);
					expect = ExpectedAnswer.HashTerminated;
				}
				else if (message.EndsWith("#,##"))
				{
					message = message.Substring(0, message.Length - 3);
					expect = ExpectedAnswer.DoubleHashTerminated;
				}
				else if (message.EndsWith("#,n"))
				{
					message = message.Substring(0, message.Length - 2);
					expect = ExpectedAnswer.Digit;
				}
				else if (!message.EndsWith("#"))
				{
					message += "#";
				}

				if (SharedSerial.Connected && !String.IsNullOrEmpty(message))
				{
					LogMessage(LoggingFlags.Serial, $"SendMessage Nr{messageNr,0:0000} - Sending command, expecting {expect} reply for '{message}'");
					SharedSerial.ClearBuffers();
					SharedSerial.Transmit(message);
					switch (expect)
					{
						case ExpectedAnswer.Digit:
							retVal = SharedSerial.ReceiveCounted(1);
							break;
						case ExpectedAnswer.HashTerminated:
							retVal = SharedSerial.ReceiveTerminated("#");
							retVal = retVal.TrimEnd('#');
							break;
						case ExpectedAnswer.DoubleHashTerminated:
							retVal = SharedSerial.ReceiveTerminated("#");
							retVal = retVal.TrimEnd('#');
							retVal = SharedSerial.ReceiveTerminated("#");
							break;
					}

					LogMessage(LoggingFlags.Serial, $"SendMessage Nr{messageNr,0:0000} - Reply: " + retVal);
				}
				else
				{
					LogMessage(LoggingFlags.Serial, $"SendMessage Nr{messageNr,0:0000} - Not connected or Empty Message: " + message);
				}
				LogMessage(LoggingFlags.Serial, $"SendMessage Nr{messageNr,0:0000} - Releasing lock");
			}

			LogMessage(LoggingFlags.Serial, $"SendMessage Nr{messageNr,0:0000} < Released lock");
			return retVal;
		}

		public static string SendPassThroughCommand(string message)
		{
			return SendMessage(message);
		}


		/// <summary>
		/// Example of handling connecting to and disconnection from the
		/// shared serial port.
		/// Needs error handling
		/// the port name etc. needs to be set up first, this could be done by the driver
		/// checking Connected and if it's false setting up the port before setting connected to true.
		/// It could also be put here.
		/// </summary>
		public static bool Connected
		{
			set
			{
				LogMessage(LoggingFlags.Server, $"Connected Set({value})");
				lock (lockObject)
				{
					LogMessage(LoggingFlags.Server, $"Connected Set - {value}");
					if (value)
					{
						if (Connections == 0)
						{
							LogMessage(LoggingFlags.Server, $"Connected Set - No connections active");
							try
							{
								SharedSerial.DTREnable = false;
								SharedSerial.PortName = ReadProfile().ComPort;
								SharedSerial.ReceiveTimeoutMs = 2000;
								SharedSerial.Speed = (SerialSpeed)ReadProfile().BaudRate;

								LogMessage(LoggingFlags.Server, $"Connected Set - Attempting to connect {SharedSerial.PortName}@{SharedSerial.Speed}");
								SharedSerial.Connected = true;

								Thread.Sleep(1000);
								LogMessage(LoggingFlags.Server, $"Connected Set - Connection seems to have succeeded.");

								SharedSerial.Transmit(":I#");
							}
							catch (System.IO.IOException exception)
							{
								MessageBox.Show("Serial port not opened for " + SharedResources.SharedSerial.PortName,
									"Invalid port state", MessageBoxButtons.OK, MessageBoxIcon.Error);
								LogMessage(LoggingFlags.Server, $"Connected Set - Serial port not opened: " + exception.Message);
							}
							catch (System.UnauthorizedAccessException exception)
							{
								MessageBox.Show("Access denied to serial port " + SharedResources.SharedSerial.PortName,
									"Access denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
								LogMessage(LoggingFlags.Server, $"Connected Set - Access denied to serial port: " + exception.Message);
							}
							catch (ASCOM.DriverAccessCOMException exception)
							{
								MessageBox.Show("ASCOM driver exception: " + exception.Message,
									"ASCOM driver exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
								LogMessage(LoggingFlags.Server, $"Connected Set - Driver Access failure: " + exception.Message);
							}
							catch (System.Runtime.InteropServices.COMException exception)
							{
								MessageBox.Show(
									"Serial port read timeout for port " + SharedResources.SharedSerial.PortName,
									"Timeout", MessageBoxButtons.OK, MessageBoxIcon.Error);
								LogMessage(LoggingFlags.Server, $"Connected Set - Serial port COM Exception: " + exception.Message);
							}
						}
						else
						{
							LogMessage(LoggingFlags.Server, $"Connected Set - Already connected with {Connections} connections active");
						}

						Connections++;
						LogMessage(LoggingFlags.Server, $"Connected Set - Connection Count is {Connections} clients");
					}
					else
					{
						Connections--;
						LogMessage(LoggingFlags.Server, $"Connected Set - Connection Count is {Connections} clients");
						if (Connections <= 0)
						{
							Connections = 0;
							LogMessage(LoggingFlags.Server, $"Connected Set - No Connections remain, sedning Qq# and disconnecting device");
							SharedSerial.Transmit(":Qq#");
							SharedSerial.Connected = false;
						}
					}
				}
			}

			get => SharedSerial.Connected;
		}


		/// <summary>
		/// Required Interface functions for ASCOM, apparently.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public static string SendMessageString(string message)
		{
			try
			{
				string answer = SharedResources.SendMessage(message);
				return answer;
			}
			catch
			{
				return "";
			}
		}

		/// <summary>
		/// Required Interface functions for ASCOM, apparently.
		/// </summary>
		public static void SendMessageBlind(string message)
		{
			try
			{
				string answer = SharedResources.SendMessage(message);
			}
			catch { }
		}

		#endregion

		/// <summary>
		/// Skeleton of a hardware class, all this does is hold a count of the connections,
		/// in reality extra code will be needed to handle the hardware in some way
		/// </summary>
		public class DeviceHardware
		{
			internal int count { set; get; }

			internal DeviceHardware()
			{
				count = 0;
			}
		}

		private static void LogMessage(LoggingFlags flag, string message)
		{
			LogMessageCallback(flag, message);
		}

		public static void SetTraceFlags(LoggingFlags flags)
		{
			currentLogFlags = flags;
		}

		public static void LogMessageCallback(LoggingFlags flag, string message)
		{
			if ((currentLogFlags & flag) != 0)
			{
				EnsureLogger();
				traceLogger.LogMessage(flag.ToString(), $"[{Thread.CurrentThread.ManagedThreadId,0:000}] {message}");
			}
		}

	}
}