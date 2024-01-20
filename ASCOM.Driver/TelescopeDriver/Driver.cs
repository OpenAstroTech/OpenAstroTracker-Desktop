using System;
using System.Threading;
using ASCOM.Astrometry.AstroUtils;
using ASCOM.DeviceInterface;
using ASCOM.Utilities;
using ASCOM.Astrometry.Transform;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Runtime.CompilerServices;
using static ASCOM.OpenAstroTracker.SharedResources;

namespace ASCOM.OpenAstroTracker
{
	[Guid("be07c02f-8a5e-429f-87b1-23fe9d5f4065")]
	[ProgId("ASCOM.OpenAstroTracker.Telescope")]
	[ServedClassName("OpenAstroTracker")]
	[ClassInterface(ClassInterfaceType.None)]
	public class Telescope : ReferenceCountedObjectBase, ITelescopeV3
	{
		// 
		// Driver ID and descriptive string that shows in the Chooser
		// 
		private string Version => GetType().Assembly.GetName().Version.ToString();
		private string _driverId;
		private static string driverDescription = "OpenAstroTracker Telescope";

		internal static double PolarisRAJNow = 0.0;
		internal static DriveRates driveRate = DriveRates.driveSidereal;

		private Util _utilities = new Util();
		private AstroUtils _astroUtilities = new AstroUtils();
		//private TraceLogger _tl;
		private Action<LoggingFlags, string> logMessageFunc;
		private Transform _transform = new Transform();
		private Transform _azAltTransform = new Transform();

		private bool _isParked;
		private bool _isTracking = true;
		private double _targetRa;
		private double _targetDec;
		private bool _targetRaSet;
		private bool _targetDecSet;
		private bool _isConnected = false;
		private long _fwVersion = 0;

		private ProfileData Profile => SharedResources.ReadProfile();

		// 
		// Constructor - Must be public for COM registration!
		// 
		public Telescope()
		{
			_driverId = Marshal.GenerateProgIdForType(this.GetType());

			logMessageFunc = SharedResources.LogMessageCallback;
			SharedResources.SetTraceFlags(Profile.TraceFlags);
			LogMessage(LoggingFlags.Scope, $"Starting initialization - v{Version}");

			// TODO: Implement your additional construction here
			_transform.SetJ2000(_utilities.HMSToHours("02:31:51.12"), _utilities.DMSToDegrees("89:15:51.4"));
			_transform.SiteElevation = SiteElevation;
			_transform.SiteLatitude = SiteLatitude;
			_transform.SiteLongitude = SiteLongitude;
			PolarisRAJNow = _transform.RATopocentric;

			_azAltTransform.SiteElevation = SiteElevation;
			_azAltTransform.SiteLatitude = SiteLatitude;
			_azAltTransform.SiteLongitude = SiteLongitude;

			LogMessage(LoggingFlags.Scope, "Completed initialization");
		}

		// 
		// PUBLIC COM INTERFACE ITelescopeV3 IMPLEMENTATION
		// 

		/// <summary>
		///     ''' Displays the Setup Dialog form.
		///     ''' If the user clicks the OK button to dismiss the form, then
		///     ''' the new settings are saved, otherwise the old values are reloaded.
		///     ''' THIS IS THE ONLY PLACE WHERE SHOWING USER INTERFACE IS ALLOWED!
		///     ''' </summary>
		public void SetupDialog()
		{
			using (var f = new SetupDialogForm(Profile, this, (s) => this.LogMessage(LoggingFlags.Setup, s)))
			{
				if (f.ShowDialog() == DialogResult.OK)
				{
					SharedResources.WriteProfile(f.GetProfileData()); // Persist device configuration values to the ASCOM Profile store
					SharedResources.SetTraceFlags(Profile.TraceFlags);
				}
			}
		}

		public ArrayList SupportedActions
		{
			get
			{
				ArrayList actionList = new ArrayList();
				actionList.Add("Telescope:getFirmwareVer");
				actionList.Add("Utility:JNowtoJ2000");
				actionList.Add("Serial:PassThroughCommand");
				LogMessage(LoggingFlags.Scope, "SupportedActions Get => " + actionList.Count.ToString() + " item(s)");
				return actionList;
			}
		}

		public string Action(string ActionName, string ActionParameters)
		{
			if (SupportedActions.Contains(ActionName))
			{
				LogMessage(LoggingFlags.Scope, $"Action > ({ActionName}, {ActionParameters}) called");
				string retVal = "255"; // Default error code
				switch (ActionName)
				{
					case "Telescope:getFirmwareVer":
					{
						retVal = CommandString(":GVP#,#"); // Get firmware name
						retVal = retVal + " " + CommandString(":GVN#,#"); // Get firmware version number
						break;
					}

					case "Utility:JNowtoJ2000":
					{
						_transform.SetTopocentric(System.Convert.ToDouble(ActionParameters.Split(',')[0]),
							System.Convert.ToDouble(ActionParameters.Split(',')[1]));
						retVal = _utilities.HoursToHMS(_transform.RAJ2000, ":", ":", string.Empty) + "&" +
									DegreesToDmsWithSign(_transform.DecJ2000, "*", ":", string.Empty);
						break;
					}

					case "Serial:PassThroughCommand":
					{
						retVal = SharedResources.SendPassThroughCommand(ActionParameters);
						break;
					}
				}
				LogMessage(LoggingFlags.Scope, $"Action < result: {retVal}");

				return retVal;
			}
			else
				throw new ActionNotImplementedException("Action " + ActionName + " is not supported by this driver");
		}

		/// <summary>
		/// Required Interface functions of ITelescopeV3 
		/// </summary>
		public void CommandBlind(string Command, bool Raw = false)
		{
			SharedResources.OATCommandMutex.WaitOne();

			try
			{
				SharedResources.SendMessage(Command);
			}
			catch (Exception ex)
			{
				LogMessage(LoggingFlags.Scope, "CommandBlind <! Exception : " + ex.Message);
			}
			finally
			{
				SharedResources.OATCommandMutex.ReleaseMutex();
			}
		}

		/// <summary>
		/// Required Interface functions of ITelescopeV3 
		/// </summary>
		public bool CommandBool(string Command, bool Raw = false)
		{
			throw new System.NotImplementedException();
		}

		/// <summary>
		/// Required Interface functions of ITelescopeV3 
		/// </summary>
		public string CommandString(string Command, bool raw = false)
		{
			SharedResources.OATCommandMutex.WaitOne();

			try
			{
				var response = SharedResources.SendMessage(Command);
				return response;
			}
			catch (Exception ex)
			{
				LogMessage(LoggingFlags.Scope, "CommandString <! Exception :" + ex.Message);
				return "255";
			}
			finally
			{
				SharedResources.OATCommandMutex.ReleaseMutex();
			}
		}

		public bool Connected
		{
			get => IsConnected;
			set
			{
				if (IsConnected != value)
				{
					_isConnected = value;
					SharedResources.Connected = value;
				}
			}
		}

		public string Description
		{
			get
			{
				LogMessage(LoggingFlags.Scope, "Description Get => " + driverDescription);
				return driverDescription;
			}
		}

		public string DriverInfo
		{
			get
			{
				string s_driverInfo = "OpenAstroTracker ASCOM driver version: " + Version;
				LogMessage(LoggingFlags.Scope, "DriverInfo Get => " + s_driverInfo);
				return s_driverInfo;
			}
		}

		public string DriverVersion
		{
			get
			{
				LogMessage(LoggingFlags.Scope, "DriverVersion Get =>" + Version);
				return Version;
			}
		}

		public short InterfaceVersion
		{
			get
			{
				LogMessage(LoggingFlags.Scope, "InterfaceVersion Get => 3");
				return 3;
			}
		}

		public string Name
		{
			get
			{
				string s_name = "OAT ASCOM";
				LogMessage(LoggingFlags.Scope, "Name Get => " + s_name);
				return s_name;
			}
		}

		public void Dispose()
		{
			Connected = false;
		}


		public void AbortSlew()
		{
			if (!AtPark)
			{
				LogMessage(LoggingFlags.Scope, "AbortSlew - not parked, sending :Q#");
				CommandBlind(":Q");
			}
			else
				throw new ASCOM.ParkedException("AbortSlew");
		}

		public AlignmentModes AlignmentMode
		{
			get
			{
				LogMessage(LoggingFlags.Scope, "AlignmentMode Get => 1 (algPolar)");
				return
					AlignmentModes.algPolar; // 1 is "Polar (equatorial) mount other than German equatorial." from AlignmentModes Enumeration
			}
		}

		public double Altitude
		{
			get
			{
				_azAltTransform.SetApparent(RightAscension, Declination);
				LogMessage(LoggingFlags.Scope, $"Altitude Get => {_azAltTransform.ElevationTopocentric:0.00}");
				return _azAltTransform.ElevationTopocentric;
			}
		}

		public double ApertureArea
		{
			get
			{
				LogMessage(LoggingFlags.Scope, "ApertureArea Get - Not implemented");
				throw new ASCOM.PropertyNotImplementedException("ApertureArea", false);
			}
		}

		public double ApertureDiameter
		{
			get
			{
				LogMessage(LoggingFlags.Scope, "ApertureDiameter Get - Not implemented");
				throw new ASCOM.PropertyNotImplementedException("ApertureDiameter", false);
			}
		}

		public bool AtHome
		{
			get
			{
				// This property must be False if the telescope does not support homing.
				// TODO : We'll try to implement homing later.
				LogMessage(LoggingFlags.Scope, "AtHome Get => false");
				return false;
			}
		}

		public bool AtPark
		{
			get
			{
				LogMessage(LoggingFlags.Scope, $"AtPark Get => {_isParked}");
				return _isParked; // Custom boolean we added to track parked state
			}
		}

		public IAxisRates AxisRates(TelescopeAxes Axis)
		{
			LogMessage(LoggingFlags.Scope, $"AxisRates Get for {Axis}");
			return new AxisRates(Axis);
		}

		public double Azimuth
		{
			get
			{
				_azAltTransform.SetApparent(RightAscension, Declination);
				LogMessage(LoggingFlags.Scope, $"Azimuth Get => Az: {_azAltTransform.AzimuthTopocentric:0.00}");
				return _azAltTransform.AzimuthTopocentric;
			}
		}

		public bool CanFindHome
		{
			get
			{
				if (!IsConnected)
					throw new ASCOM.NotConnectedException("CanFindHome");
				LogMessage(LoggingFlags.Scope, "CanFindHome Get => false");
				return false;
			}
		}

		public bool CanMoveAxis(TelescopeAxes Axis)
		{
			switch (Axis)
			{
				case TelescopeAxes.axisPrimary:
				case TelescopeAxes.axisSecondary:
					LogMessage(LoggingFlags.Scope, "CanMoveAxis(" + Axis.ToString() + ") => true");
					return true;
				default:
					LogMessage(LoggingFlags.Scope, "CanMoveAxis(" + Axis.ToString() + ") => false");
					return false;
			}
		}

		public bool CanPark
		{
			get
			{
				LogMessage(LoggingFlags.Scope, "CanPark Get => true");
				return true;
			}
		}

		public bool CanPulseGuide
		{
			get
			{
				LogMessage(LoggingFlags.Scope, "CanPulseGuide Get => true");
				return true;
			}
		}

		public bool CanSetDeclinationRate
		{
			get
			{
				LogMessage(LoggingFlags.Scope, "CanSetDeclinationRate Get => false");
				return false;
			}
		}

		public bool CanSetGuideRates
		{
			get
			{
				LogMessage(LoggingFlags.Scope, "CanSetGuideRates Get => false");
				return false;
			}
		}

		public bool CanSetPark
		{
			// ToDo  We should allow this
			get
			{
				LogMessage(LoggingFlags.Scope, "CanSetPark Get => false");
				return false;
			}
		}

		public bool CanSetPierSide
		{
			get
			{
				LogMessage(LoggingFlags.Scope, "CanSetPierSide Get => false");
				return false;
			}
		}

		public bool CanSetRightAscensionRate
		{
			get
			{
				LogMessage(LoggingFlags.Scope, "CanSetRightAscensionRate Get => false");
				return false;
			}
		}

		public bool CanSetTracking
		{
			get
			{
				LogMessage(LoggingFlags.Scope, "CanSetTracking Get => true");
				return true;
			}
		}

		public bool CanSlew
		{
			get
			{
				LogMessage(LoggingFlags.Scope, "CanSlew Get => true");
				return true;
			}
		}

		public bool CanSlewAltAz
		{
			// TODO - AltAz slewing
			get
			{
				LogMessage(LoggingFlags.Scope, "CanSlewAltAz Get => false");
				return false;
			}
		}

		public bool CanSlewAltAzAsync
		{
			get
			{
				LogMessage(LoggingFlags.Scope, "CanSlewAltAzAsync Get => false");
				return false;
			}
		}

		public bool CanSlewAsync
		{
			get
			{
				LogMessage(LoggingFlags.Scope, "CanSlewAsync Get => true");
				return true;
			}
		}

		public bool CanSync
		{
			get
			{
				LogMessage(LoggingFlags.Scope, "CanSync Get => true");
				return true;
			}
		}

		public bool CanSyncAltAz
		{
			get
			{
				LogMessage(LoggingFlags.Scope, "CanSyncAltAz Get => false");
				return false;
			}
		}

		public bool CanUnpark
		{
			get
			{
				LogMessage(LoggingFlags.Scope, "CanUnpark Get => true");
				return true;
			}
		}

		public double Declination
		{
			get
			{
				LogMessage(LoggingFlags.Scope, $"Declination Get");
				double declination = 0.0;
				string scopeDec = CommandString(":GD#,#");
				declination = _utilities.DMSToDegrees(scopeDec);
				LogMessage(LoggingFlags.Scope, $"Declination Get => {declination:0.00}  (Raw:{scopeDec})");
				if (!_targetDecSet)
				{
					TargetDeclination = declination;
				}

				return declination;
			}
		}

		public double DeclinationRate
		{
			get
			{
				double declination = 0.0;
				LogMessage(LoggingFlags.Scope, $"DeclinationRate Get => {declination:0.00}");
				return declination;
			}
			set
			{
				LogMessage(LoggingFlags.Scope, "DeclinationRate Set - Not implemented");
				throw new ASCOM.PropertyNotImplementedException("DeclinationRate", true);
			}
		}

		public PierSide DestinationSideOfPier(double RightAscension, double Declination)
		{
			LogMessage(LoggingFlags.Scope, "DestinationSideOfPier Get - Not implemented");
			throw new ASCOM.MethodNotImplementedException("DestinationSideOfPier");
		}

		public bool DoesRefraction
		{
			get
			{
				LogMessage(LoggingFlags.Scope, "DoesRefraction Get - Not implemented");
				throw new ASCOM.PropertyNotImplementedException("DoesRefraction", false);
			}
			set
			{
				LogMessage(LoggingFlags.Scope, "DoesRefraction Set - Not implemented");
				throw new ASCOM.PropertyNotImplementedException("DoesRefraction", true);
			}
		}

		public EquatorialCoordinateType EquatorialSystem
		{
			// TODO : Determine if we're using JNow or J2000, or can use both.  Work on this.
			get
			{
				EquatorialCoordinateType equatorialSystem = EquatorialCoordinateType.equTopocentric;
				LogMessage(LoggingFlags.Scope, $"DeclinationRate Get => Topocentric");
				return equatorialSystem;
			}
		}

		public void FindHome()
		{
			LogMessage(LoggingFlags.Scope, "FindHome - Not implemented");
			throw new ASCOM.MethodNotImplementedException("FindHome");
		}

		public double FocalLength
		{
			get
			{
				LogMessage(LoggingFlags.Scope, "FocalLength Get - Not implemented");
				throw new ASCOM.PropertyNotImplementedException("FocalLength", false);
			}
		}

		public double GuideRateDeclination
		{
			get
			{
				LogMessage(LoggingFlags.Scope, "GuideRateDeclination Get - Not implemented");
				throw new ASCOM.PropertyNotImplementedException("GuideRateDeclination", false);
			}
			set
			{
				LogMessage(LoggingFlags.Scope, "GuideRateDeclination Set - Not implemented");
				throw new ASCOM.PropertyNotImplementedException("GuideRateDeclination", true);
			}
		}

		public double GuideRateRightAscension
		{
			get
			{
				LogMessage(LoggingFlags.Scope, "GuideRateRightAscension Get - Not implemented");
				throw new ASCOM.PropertyNotImplementedException("GuideRateRightAscension", false);
			}
			set
			{
				LogMessage(LoggingFlags.Scope, "GuideRateRightAscension Set - Not implemented");
				throw new ASCOM.PropertyNotImplementedException("GuideRateRightAscension", true);
			}
		}

		public bool IsPulseGuiding
		{
			get
			{
				LogMessage(LoggingFlags.Scope, $"IsPulseGuiding Get");
				bool retVal = Convert.ToBoolean(System.Convert.ToInt32(CommandString(":GIG#,#")));
				LogMessage(LoggingFlags.Scope, "IsPulseGuiding Get => " + retVal.ToString());
				return retVal;
			}
		}


		private bool _trackingPriorToMove;
		private double _ratePriorToMove;
		public void MoveAxis(TelescopeAxes Axis, double Rate)
		{
			LogMessage(LoggingFlags.Scope, $"MoveAxis({Axis}, {Rate:0.00})");

			if (Axis == TelescopeAxes.axisTertiary)
			{
				LogMessage(LoggingFlags.Scope, "MoveAxis - Axis not supported.");
				throw new ASCOM.NotImplementedException("MoveAxis Tertiary Not Supported.");
			}

			if (AtPark)
			{
				LogMessage(LoggingFlags.Scope, "MoveAxis - Scope is parked");
				throw new ASCOM.ParkedException("MoveAxis");
			}

			if (Rate != 0 && !ValidAxisSpeed(Axis, Rate))
			{
				LogMessage(LoggingFlags.Scope, $"MoveAxis - invalid speed {Rate:0.000} for axis {Axis}");
				throw new ASCOM.InvalidValueException("Invalid speed for Axis");
			}

			var sAxis = Enum.GetName(typeof(TelescopeAxes), Axis);
			string cmd = "Q";


			if (Rate == 0)
			{
				LogMessage(LoggingFlags.Scope, $"MoveAxis - {sAxis} Rate is zero, so stopping Slew and setting Rate S");
				// if at some point we support multiple tracking rates this should set
				// the value back to the previous rate...
				CommandBlind($":{cmd}");
				// Restore slewing rate to max
				CommandBlind($":RS");
				// Set tracking state to before
				Tracking = _trackingPriorToMove;
			}
			else
			{
				cmd = "S";
				_trackingPriorToMove = Tracking;
				//_ratePriorToMove = Rate;
				double rate = Math.Abs(Rate);
				string rateCommandParam = "GCMS";
				int index = 0;
				LogMessage(LoggingFlags.Scope, $"MoveAxis - {sAxis} Finding Rate for {rate}");
				foreach (Rate availRate in AxisRates(Axis))
				{
					LogMessage(LoggingFlags.Scope, $"MoveAxis - {sAxis} Check rate {index} : {availRate.Minimum} -> {availRate.Maximum}");

					if (rate < availRate.Maximum)
					{
						cmd = rateCommandParam.Substring(index, 1);
						LogMessage(LoggingFlags.Scope, $"MoveAxis - {sAxis} Rate found. Using mode {cmd}");
						break;
					}
					index++;
				}

				CommandBlind($":R{cmd}");

				switch (Axis)
				{
					case TelescopeAxes.axisPrimary:
						cmd = Rate > 0 ? "Mw" : "Me";
						break;
					case TelescopeAxes.axisSecondary:
						cmd = Rate > 0 ? "Mn" : "Ms";
						break;
				}

				CommandBlind($":{cmd}");
				LogMessage(LoggingFlags.Scope, $"MoveAxis() complete");
			}
		}

		private bool ValidAxisSpeed(TelescopeAxes axis, double reqRate)
		{
			var rates = AxisRates(axis);
			var absRate = Math.Abs(reqRate);
			foreach (Rate rate in rates)
			{
				if (absRate >= rate.Minimum && absRate <= rate.Maximum)
				{
					return true;
				}
			}

			return false;
		}

		public void Park()
		{
			if (!AtPark)
			{
				LogMessage(LoggingFlags.Scope, $"Park() called");
				CommandBlind(":hP");
				PollUntilZero(":GIS#,#");
				_isParked = true;
				LogMessage(LoggingFlags.Scope, "Park() - Parked mount");
			}
		}

		public void PulseGuide(GuideDirections Direction, int Duration)
		{
			if (!AtPark)
			{
				var dirString = Enum.GetName(typeof(GuideDirections), Direction);
				var durString = Duration.ToString("0000");
				LogMessage(LoggingFlags.Scope, $"PulseGuide ({Direction}, {durString}ms)");
				var dir = dirString.Substring(5, 1).ToLower();
				CommandBlind($":Mg{dir}{durString}");
				LogMessage(LoggingFlags.Scope, $"PulseGuide complete");
			}
			else
			{
				LogMessage(LoggingFlags.Scope, "PulseGuide - Scope is parked");
				throw new ASCOM.ParkedException("PulseGuide");
			}
		}

		public double RightAscension
		{
			get
			{
				double rightAscension = 0.0;
				LogMessage(LoggingFlags.Scope, $"RightAscension Get");
				rightAscension = _utilities.HMSToHours(CommandString(":GR#,#"));
				LogMessage(LoggingFlags.Scope, $"RightAscension Get => {rightAscension:0.000}");
				if (!_targetRaSet)
				{
					TargetRightAscension = rightAscension;
				}
				return rightAscension;
			}
		}

		public double RightAscensionRate
		{
			get
			{
				double rightAscensionRate = 0.0;
				LogMessage(LoggingFlags.Scope, $"RightAscensionRate Get => {rightAscensionRate:0.000}");
				return rightAscensionRate;
			}
			set
			{
				LogMessage(LoggingFlags.Scope, "RightAscensionRate Set - Not implemented");
				throw new ASCOM.PropertyNotImplementedException("RightAscensionRate", true);
			}
		}

		public void SetPark()
		{
			LogMessage(LoggingFlags.Scope, "SetPark - Not implemented");
			throw new ASCOM.MethodNotImplementedException("SetPark");
		}

		public PierSide SideOfPier
		{
			get
			{
				PierSide retVal;
				if (SiderealTime < 12)
				{
					if (RightAscension >= SiderealTime && RightAscension <= SiderealTime + 12)
						retVal = PierSide.pierWest;
					else
						retVal = PierSide.pierEast;
				}
				else if (RightAscension <= SiderealTime && RightAscension >= SiderealTime - 12)
					retVal = PierSide.pierEast;
				else
					retVal = PierSide.pierWest;

				LogMessage(LoggingFlags.Scope, $"SideOfPier Get => {Enum.GetName(typeof(PierSide), retVal)}");
				return retVal;
			}
			set
			{
				LogMessage(LoggingFlags.Scope, "SideOfPier Set - Not Implemented");
				throw new ASCOM.PropertyNotImplementedException("SideOfPier", true);
			}
		}

		public double SiderealTime
		{
			get
			{
				// now using novas 3.1
				double lst = 0.0;
				using (ASCOM.Astrometry.NOVAS.NOVAS31 novas = new ASCOM.Astrometry.NOVAS.NOVAS31())
				{
					double jd = _utilities.DateUTCToJulian(DateTime.UtcNow);
					novas.SiderealTime(jd, 0, novas.DeltaT(jd), ASCOM.Astrometry.GstType.GreenwichMeanSiderealTime,
						ASCOM.Astrometry.Method.EquinoxBased, ASCOM.Astrometry.Accuracy.Reduced, ref lst);
				}

				// Allow for the longitude
				lst += SiteLongitude / 360.0 * 24.0;

				// Reduce to the range 0 to 24 hours
				lst = _astroUtilities.ConditionRA(lst);
				LogMessage(LoggingFlags.Scope, $"SiderealTime Get => {lst:0.000}");
				return lst;
			}
		}

		public double SiteElevation
		{
			get
			{
				LogMessage(LoggingFlags.Scope, $"SiteElevation Get => {Profile.Elevation:0.0}");
				return Profile.Elevation;
			}
			set
			{
				if (value >= -300 && value <= 10000)
				{
					LogMessage(LoggingFlags.Scope, $"SiteElevation Set {value:0.0}");
					var profile = Profile;
					if (profile.Elevation != value)
					{
						LogMessage(LoggingFlags.Scope, $"SiteElevation Setting from {profile.Elevation:0.00} to {value:0.00}");
						profile.Elevation = value;
						SharedResources.WriteProfile(profile);
						_transform.SiteElevation = SiteElevation;
						_azAltTransform.SiteElevation = SiteElevation;
					}
				}
				else
				{
					LogMessage(LoggingFlags.Scope, $"SiteElevation Set {value:0.0} out of range, Atlantis or Mt Everest no supported.");
					throw new ASCOM.InvalidValueException("SiteElevation");
				}
			}
		}

		public double SiteLatitude
		{
			get
			{
				LogMessage(LoggingFlags.Scope, $"SiteLatitude Get => {Profile.Latitude:0.00}");
				return Profile.Latitude;
			}
			set
			{
				LogMessage(LoggingFlags.Scope, $"SiteLatitude Set => {value:0.00}");
				var profile = Profile;
				if (profile.Latitude != value)
				{
					LogMessage(LoggingFlags.Scope, $"SiteLatitude Setting from {profile.Latitude:0.00} to {value:0.00}");
					profile.Latitude = value;
					SharedResources.WriteProfile(profile);
					_transform.SiteLatitude = SiteLatitude;
					_azAltTransform.SiteLatitude = SiteLatitude;
				}
			}
		}

		public double SiteLongitude
		{
			get
			{
				LogMessage(LoggingFlags.Scope, $"SiteLongitude Get => {Profile.Longitude:0.00}");
				return Profile.Longitude;
			}
			set
			{
				var profile = Profile;
				if (profile.Longitude!= value)
				{
					LogMessage(LoggingFlags.Scope, $"SiteLongitude Setting from {profile.Longitude:0.00} to {value:0.00}");
					profile.Longitude = value;
					SharedResources.WriteProfile(profile);
					_transform.SiteLongitude = SiteLongitude;
					_azAltTransform.SiteLongitude = SiteLongitude;
				}
			}
		}

		public long FirmwareVersion
		{
			get
			{
				if (!_isConnected)
				{
					throw new ASCOM.NotConnectedException();
				}

				LogMessage(LoggingFlags.Scope, "FirmwareVersion Get");
				if (_fwVersion == 0)
				{
					var version = CommandString(":GVN#,#");
					var versionNumbers = version.Substring(1).Split(".".ToCharArray());
					if (versionNumbers.Length != 3)
					{
						LogMessage(LoggingFlags.Scope, $"Unrecognizable firmware version '{version}'");
					}
					else
					{
						try
						{
							_fwVersion = long.Parse(versionNumbers[0]) * 10000L + long.Parse(versionNumbers[1]) * 100L + long.Parse(versionNumbers[2]);
						}
						catch
						{
							LogMessage(LoggingFlags.Scope, $"Unable to parse firmware version '{version}'");
						}
					}
				}
				LogMessage(LoggingFlags.Scope, $"FirmwareVersion Get => {_fwVersion}");
				return _fwVersion;
			}
		}

		public short SlewSettleTime
		{
			get
			{
				LogMessage(LoggingFlags.Scope, "SlewSettleTime Get - Not implemented");
				throw new ASCOM.PropertyNotImplementedException("SlewSettleTime", false);
			}
			set
			{
				LogMessage(LoggingFlags.Scope, "SlewSettleTime Set - Not implemented");
				throw new ASCOM.PropertyNotImplementedException("SlewSettleTime", true);
			}
		}

		public void SlewToAltAz(double Azimuth, double Altitude)
		{
			LogMessage(LoggingFlags.Scope, "SlewToAltAz - Not implemented");
			throw new ASCOM.MethodNotImplementedException("SlewToAltAz");
		}

		public void SlewToAltAzAsync(double Azimuth, double Altitude)
		{
			LogMessage(LoggingFlags.Scope, "SlewToAltAzAsync - Not implemented");
			throw new ASCOM.MethodNotImplementedException("SlewToAltAzAsync");
		}

		public void SlewToCoordinates(double ra, double dec) =>
			SlewToCoordinatesWithWait("SlewToCoordinates", ra, dec, true);

		private void SlewToCoordinatesWithWait(string caller, double ra, double dec, bool wait)
		{
			if (AtPark)
			{
				LogMessage(LoggingFlags.Scope, $"{caller} - Scope is parked!");
				throw new ASCOM.ParkedException(caller);
			}

			if (ValidateCoordinates(ra, dec))
			{
				LogMessage(LoggingFlags.Scope, $"{caller} - RA {ra:0.000},  Dec {dec:0.000}");
				SetTargetCoordinates(ra, dec);
				CommandString(":MS#,n");
				if (wait)
				{
					PollUntilZero(":GIS#,#");
				}
			}
			else
			{
				LogMessage(LoggingFlags.Scope, $"{caller} - Invalid coordinates RA: " + ra.ToString() + ", Dec: " + dec.ToString());
				throw new ASCOM.InvalidValueException(caller);
			}
		}

		private bool SetTargetCoordinates(double rightAscension, double declination)
		{
			var strRAcmd = $":Sr{_utilities.HoursToHMS(rightAscension, ":", ":")}#,n";
			var strDeccmd = $":Sd{DegreesToDmsWithSign(declination, "*", ":", "")}#,n";
			LogMessage(LoggingFlags.Scope, $"SetTargetCoordinates - RA : {rightAscension:0.000}, DEC: {declination:0.00}");

			TargetRightAscension = rightAscension;
			TargetDeclination = declination;

			return CommandString(strRAcmd) == "1" && CommandString(strDeccmd) == "1";
		}

		public void SlewToCoordinatesAsync(double ra, double dec) =>
			SlewToCoordinatesWithWait("SlewToCoordinatesAsync", ra, dec, false);

		public void SlewToTarget()
		{
			if (_targetRaSet && _targetDecSet)
			{
				LogMessage(LoggingFlags.Scope, $"SlewToTarget - RA: {TargetRightAscension:0.000}, DEC: {TargetDeclination:0.000}");
				SlewToCoordinates(TargetRightAscension, TargetDeclination);
			}
			else
			{
				LogMessage(LoggingFlags.Scope, $"SlewToTarget - No Target RA, DEC set");
				throw new InvalidValueException("TargetRightAscension or TargetDeclination are not set or valid");
			}
		}

		public void SlewToTargetAsync()
		{
			if (_targetRaSet && _targetDecSet)
			{
				LogMessage(LoggingFlags.Scope, $"SlewToTargetAsync - RA: {TargetRightAscension:0.000}, DEC: {TargetDeclination:0.000}");
				SlewToCoordinatesAsync(TargetRightAscension, TargetDeclination);
			}
			else
			{
				throw new InvalidValueException("TargetRightAscension or TargetDeclination are not set or valid");
			}
		}

		public bool Slewing
		{
			get
			{
				LogMessage(LoggingFlags.Scope, $"Slewing Get");

				bool retVal = Convert.ToBoolean(System.Convert.ToInt32(CommandString(":GIS#,#")));
				LogMessage(LoggingFlags.Scope, "Slewing Get => " + retVal.ToString());
				return retVal;
			}
		}

		public void SyncToAltAz(double Azimuth, double Altitude)
		{
			LogMessage(LoggingFlags.Scope, "SyncToAltAz - Not implemented");
			throw new ASCOM.MethodNotImplementedException("SyncToAltAz");
		}

		public void SyncToCoordinates(double RightAscension, double Declination)
		{
			if (AtPark)
			{
				LogMessage(LoggingFlags.Scope, "SyncToCoordinates - Scope is parked!");
				throw new ASCOM.ParkedException("SyncToCoordinates");
			}

			if (ValidateCoordinates(RightAscension, Declination))
			{
				LogMessage(LoggingFlags.Scope, $"SyncToCoordinates - RA: {TargetRightAscension:0.000}, DEC: {TargetDeclination:0.000}");
				SetTargetCoordinates(RightAscension, Declination);
				CommandString(":CM#,#");
			}
			else
			{
				LogMessage(LoggingFlags.Scope, "SyncToCoordinates - Invalid coordinates RA: " + RightAscension.ToString() + ", Dec: " + Declination.ToString());
				throw new ASCOM.InvalidValueException("SyncToCoordinates");
			}
			LogMessage(LoggingFlags.Scope, "SyncToCoordinates - complete");
		}

		private bool ValidateCoordinates(double rightAscension, double declination)
		{
			return rightAscension <= 24 && rightAscension >= 0 && declination >= -90 && declination <= 90;
		}

		public void SyncToTarget()
		{
			if (_targetDecSet && _targetRaSet)
			{
				SyncToCoordinates(TargetRightAscension, TargetDeclination);
			}
		}

		public double TargetDeclination
		{
			get
			{
				if (_targetDecSet)
				{
					LogMessage(LoggingFlags.Scope, $"TargetDeclination Get => {_targetDec:0.000}");
					return _targetDec;
				}
				else
				{
					LogMessage(LoggingFlags.Scope, "TargetDeclination Get => Value not set");
					throw new ASCOM.ValueNotSetException("TargetDeclination");
				}
			}
			set
			{
				if (value >= -90 && value <= 90)
				{
					LogMessage(LoggingFlags.Scope, $"TargetDeclination Set => {value:0.000}");
					_targetDec = value;
					_targetDecSet = true;
				}
				else
				{
					LogMessage(LoggingFlags.Scope, $"TargetDeclination Set - Invalid Value {value:0.000}");
					throw new ASCOM.InvalidValueException("TargetDeclination");
				}
			}
		}

		public double TargetRightAscension
		{
			get
			{
				if (_targetRaSet)
				{
					LogMessage(LoggingFlags.Scope, $"TargetRightAscension Get => {_targetRa:0.000}");
					return _targetRa;
				}
				else
				{
					LogMessage(LoggingFlags.Scope, "TargetRightAscension Get => Value not set");
					throw new ASCOM.ValueNotSetException("TargetRightAscension");
				}
			}
			set
			{
				if (value >= 0 && value <= 24)
				{
					LogMessage(LoggingFlags.Scope, $"TargetRightAscension Set - {value:0.000}");
					_targetRa = value;
					_targetRaSet = true;
				}
				else
				{
					LogMessage(LoggingFlags.Scope, $"TargetRightAscension Set - Invalid Value {value:0.000}");
					throw new ASCOM.InvalidValueException("TargetRightAscension");
				}
			}
		}

		public bool Tracking
		{
			get
			{
				LogMessage(LoggingFlags.Scope, "Tracking Get");

				if (CommandString(":GIT#,#") == "0")
				{
					_isTracking = false;
					LogMessage(LoggingFlags.Scope, "Tracking Get => false");
				}
				else
				{
					_isTracking = true;
					LogMessage(LoggingFlags.Scope, "Tracking Get => true");
				}

				return _isTracking;
			}
			set
			{
				LogMessage(LoggingFlags.Scope, $"Tracking - Set {value}");
				if (CommandString($":MT{Convert.ToInt32(value)}#,n") == "1")
				{
					_isTracking = value;
					LogMessage(LoggingFlags.Scope, $"Tracking Set - complete");
				}
				else
				{
					LogMessage(LoggingFlags.Scope, "Tracking Set - Error, OAT returned non-1 for :MTx# command");
					throw new ASCOM.DriverException("Error setting tracking state");
				}
			}
		}

		public DriveRates TrackingRate
		{
			get
			{
				LogMessage(LoggingFlags.Scope, $"TrackingRate Get => {Enum.GetName(typeof(DriveRates), driveRate)}");
				return driveRate;
			}
			set
			{
				LogMessage(LoggingFlags.Scope, $"TrackingRate Set - Ignoring value {value}. Only sidereal supported.");
				driveRate = DriveRates.driveSidereal;
			}
		}

		public ITrackingRates TrackingRates
		{
			get
			{
				ITrackingRates trackingRates = new TrackingRates();
				string rates = string.Join(", ", trackingRates.ToString());
				LogMessage(LoggingFlags.Scope, "TrackingRates Get => " + rates);
				return trackingRates;
			}
		}

		public DateTime UTCDate
		{
			// ToDo - Can we handle this without bothering the mount?
			get
			{
				DateTime utcDate = DateTime.UtcNow;
				LogMessage(LoggingFlags.Scope, $"UTCDate Get => {utcDate}");
				return utcDate;
			}
			set { throw new ASCOM.PropertyNotImplementedException("UTCDate", true); }
		}

		public void Unpark()
		{
			if (AtPark)
			{
				LogMessage(LoggingFlags.Scope, "Unpark() called");
				string unprkRet = CommandString(":hU#,n");
				if (unprkRet == "1")
				{
					LogMessage(LoggingFlags.Scope, "Unpark - Unparked mount");
				}
				_isParked = false;
			}
		}

		private string DegreesToDmsWithSign(double degrees, string degSep = "�", string minSep = "'", string secondSep = "\"")
		{
			return (degrees >= 0 ? "+" : "") + _utilities.DegreesToDMS(degrees, degSep, minSep, secondSep);
		}

		// here are some useful properties and methods that can be used as required
		// to help with

#if INPROCESS
        private static void RegUnregASCOM(bool bRegister) {
            using (Profile P = new Profile() {DeviceType = "Telescope"}) {
                if (bRegister)
                    P.Register(driverID, driverDescription);
                else
                    P.Unregister(driverID);
            }
        }

        [ComRegisterFunction()]
        public static void RegisterASCOM(Type T) {
            RegUnregASCOM(true);
        }

        [ComUnregisterFunction()]
        public static void UnregisterASCOM(Type T) {
            RegUnregASCOM(false);
        }
#endif

		/// <summary>
		///     ''' Returns true if there is a valid connection to the driver hardware
		///     ''' </summary>
		// TODO check that the driver hardware connection exists and is connected to the hardware
		private bool IsConnected => SharedResources.Connected && _isConnected;

		/// <summary>
		///     ''' Use this function to throw an exception if we aren't connected to the hardware
		///     ''' </summary>
		///     ''' <param name="message"></param>
		private void CheckConnected(string message)
		{
			if (!IsConnected)
				throw new NotConnectedException(message);
		}

		private int PollUntilZero(string command)
		{
			// Takes a command to be sent via CommandString, and resends every 1000ms until a 0 is returned.  Returns 0 only when complete.
			string retVal = "";
			while (retVal != "0")
			{
				retVal = CommandString(command);
				Thread.Sleep(1000);
			}

			return System.Convert.ToInt32(retVal);
		}

		private void LogMessage(LoggingFlags flags, string message)
		{
			logMessageFunc(flags, message);
		}
	}
}
