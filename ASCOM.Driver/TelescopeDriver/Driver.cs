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
		private TraceLogger _tl;
		private Transform _transform = new Transform();
		private Transform _azAltTransform = new Transform();

		private bool _isParked;
		private bool _isTracking = true;
		private double _targetRa;
		private double _targetDec;
		private bool _targetRaSet;
		private bool _targetDecSet;
		private bool _isConnected = false;

		private ProfileData Profile => SharedResources.ReadProfile();

		// 
		// Constructor - Must be public for COM registration!
		// 
		public Telescope()
		{
			_driverId = Marshal.GenerateProgIdForType(this.GetType());

			_tl = SharedResources.tl;
			_tl.Enabled = Profile.TraceState;
			LogMessage("OAT Scope", $"Starting initialization - v{Version}");

			// TODO: Implement your additional construction here
			_transform.SetJ2000(_utilities.HMSToHours("02:31:51.12"), _utilities.DMSToDegrees("89:15:51.4"));
			_transform.SiteElevation = SiteElevation;
			_transform.SiteLatitude = SiteLatitude;
			_transform.SiteLongitude = SiteLongitude;
			PolarisRAJNow = _transform.RATopocentric;

			_azAltTransform.SiteElevation = SiteElevation;
			_azAltTransform.SiteLatitude = SiteLatitude;
			_azAltTransform.SiteLongitude = SiteLongitude;

			LogMessage("OAT Scope", "Completed initialization");
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
			// consider only showing the setup dialog if not connected
			// or call a different dialog if connected
			if (IsConnected)
			{
				MessageBox.Show("Already connected, just press OK");
			}

			using (var f = new SetupDialogForm(Profile, this, (s) => this.LogMessage("SetupForm", s)))
			{
				if (f.ShowDialog() == DialogResult.OK)
				{
					SharedResources.WriteProfile(f.GetProfileData()); // Persist device configuration values to the ASCOM Profile store
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
				LogMessage("OAT Scope", "SupportedActions Get => " + actionList.Count.ToString() + " item(s)");
				return actionList;
			}
		}

		public string Action(string ActionName, string ActionParameters)
		{
			if (SupportedActions.Contains(ActionName))
			{
				LogMessage("OAT Scope", $"Action({ActionName}, {ActionParameters}) called");
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
				LogMessage("OAT Scope", $"Action => {retVal}");

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
				LogMessage("OAT Scope", $"CommandBlind('{Command}') - Sending");
				SharedResources.SendMessage(Command);
			}
			catch (Exception ex)
			{
				LogMessage("OAT Scope", "CommandBlind - Error : " + ex.Message);
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
				LogMessage("OAT Scope", $"CommandString({Command} - Sending");
				var response = SharedResources.SendMessage(Command);
				LogMessage("OAT Scope", "CommandString - Received    '" + response + "'");
				return response;
			}
			catch (Exception ex)
			{
				LogMessage("OAT Scope", "CommandString - Exception :" + ex.Message);
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
				LogMessage("OAT Scope", "Description Get => " + driverDescription);
				return driverDescription;
			}
		}

		public string DriverInfo
		{
			get
			{
				string s_driverInfo = "OpenAstroTracker ASCOM driver version: " + Version;
				LogMessage("OAT Scope", "DriverInfo Get => " + s_driverInfo);
				return s_driverInfo;
			}
		}

		public string DriverVersion
		{
			get
			{
				LogMessage("OAT Scope", "DriverVersion Get =>" + Version);
				return Version;
			}
		}

		public short InterfaceVersion
		{
			get
			{
				LogMessage("OAT Scope", "InterfaceVersion Get => 3");
				return 3;
			}
		}

		public string Name
		{
			get
			{
				string s_name = "OAT ASCOM";
				LogMessage("OAT Scope", "Name Get => " + s_name);
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
				LogMessage("OAT Scope", "AbortSlew - not parked, sending :Q#");
				CommandBlind(":Q");
			}
			else
				throw new ASCOM.ParkedException("AbortSlew");
		}

		public AlignmentModes AlignmentMode
		{
			get
			{
				LogMessage("OAT Scope", "AlignmentMode Get => 1 (algPolar)");
				return
					AlignmentModes.algPolar; // 1 is "Polar (equatorial) mount other than German equatorial." from AlignmentModes Enumeration
			}
		}

		public double Altitude
		{
			get
			{
				_azAltTransform.SetApparent(RightAscension, Declination);
				LogMessage("OAT Scope", $"Altitude Get => {_azAltTransform.ElevationTopocentric:0.00}");
				return _azAltTransform.ElevationTopocentric;
			}
		}

		public double ApertureArea
		{
			get
			{
				LogMessage("OAT Scope", "ApertureArea Get - Not implemented");
				throw new ASCOM.PropertyNotImplementedException("ApertureArea", false);
			}
		}

		public double ApertureDiameter
		{
			get
			{
				LogMessage("OAT Scope","ApertureDiameter Get - Not implemented");
				throw new ASCOM.PropertyNotImplementedException("ApertureDiameter", false);
			}
		}

		public bool AtHome
		{
			get
			{
				// This property must be False if the telescope does not support homing.
				// TODO : We'll try to implement homing later.
				LogMessage("OAT Scope", "AtHome Get => false");
				return false;
			}
		}

		public bool AtPark
		{
			get
			{
				LogMessage("OAT Scope", $"AtPark Get => {_isParked}");
				return _isParked; // Custom boolean we added to track parked state
			}
		}

		public IAxisRates AxisRates(TelescopeAxes Axis)
		{
			LogMessage("OAT Scope", $"AxisRates Get for {Axis}");
			return new AxisRates(Axis);
		}

		public double Azimuth
		{
			get
			{
				_azAltTransform.SetApparent(RightAscension, Declination);
				LogMessage("OAT Scope", $"Azimuth Get => Az: {_azAltTransform.AzimuthTopocentric:0.00}");
				return _azAltTransform.AzimuthTopocentric;
			}
		}

		public bool CanFindHome
		{
			get
			{
				if (!IsConnected)
					throw new ASCOM.NotConnectedException("CanFindHome");
				LogMessage("OAT Scope", "CanFindHome Get => false");
				return false;
			}
		}

		public bool CanMoveAxis(TelescopeAxes Axis)
		{
			switch (Axis)
			{
				case TelescopeAxes.axisPrimary:
				case TelescopeAxes.axisSecondary:
					LogMessage("OAT Scope", "CanMoveAxis(" + Axis.ToString() + ") => true");
					return true;
				default:
					LogMessage("OAT Scope", "CanMoveAxis(" + Axis.ToString() + ") => false");
					return false;
			}
		}

		public bool CanPark
		{
			get
			{
				LogMessage("OAT Scope", "CanPark Get => true");
				return true;
			}
		}

		public bool CanPulseGuide
		{
			get
			{
				LogMessage("OAT Scope", "CanPulseGuide Get => true");
				return true;
			}
		}

		public bool CanSetDeclinationRate
		{
			get
			{
				LogMessage("OAT Scope", "CanSetDeclinationRate Get => false");
				return false;
			}
		}

		public bool CanSetGuideRates
		{
			get
			{
				LogMessage("OAT Scope", "CanSetGuideRates Get => false");
				return false;
			}
		}

		public bool CanSetPark
		{
			// ToDo  We should allow this
			get
			{
				LogMessage("OAT Scope", "CanSetPark Get => false");
				return false;
			}
		}

		public bool CanSetPierSide
		{
			get
			{
				LogMessage("OAT Scope", "CanSetPierSide Get => false");
				return false;
			}
		}

		public bool CanSetRightAscensionRate
		{
			get
			{
				LogMessage("OAT Scope", "CanSetRightAscensionRate Get => false");
				return false;
			}
		}

		public bool CanSetTracking
		{
			get
			{
				LogMessage("OAT Scope", "CanSetTracking Get => true");
				return true;
			}
		}

		public bool CanSlew
		{
			get
			{
				LogMessage("OAT Scope", "CanSlew Get => true");
				return true;
			}
		}

		public bool CanSlewAltAz
		{
			// TODO - AltAz slewing
			get
			{
				LogMessage("OAT Scope", "CanSlewAltAz Get => false");
				return false;
			}
		}

		public bool CanSlewAltAzAsync
		{
			get
			{
				LogMessage("OAT Scope", "CanSlewAltAzAsync Get => false");
				return false;
			}
		}

		public bool CanSlewAsync
		{
			get
			{
				LogMessage("OAT Scope", "CanSlewAsync Get => true");
				return true;
			}
		}

		public bool CanSync
		{
			get
			{
				LogMessage("OAT Scope", "CanSync Get => true");
				return true;
			}
		}

		public bool CanSyncAltAz
		{
			get
			{
				LogMessage("OAT Scope", "CanSyncAltAz Get => false");
				return false;
			}
		}

		public bool CanUnpark
		{
			get
			{
				LogMessage("OAT Scope", "CanUnpark Get => true");
				return true;
			}
		}

		public double Declination
		{
			get
			{
				double declination = 0.0;
				string scopeDec = CommandString(":GD#,#");
				declination = _utilities.DMSToDegrees(scopeDec);
				LogMessage("OAT Scope", $"Declination Get => {declination:0.00}  (Raw:{scopeDec})");
				return declination;
			}
		}

		public double DeclinationRate
		{
			get
			{
				double declination = 0.0;
				LogMessage("OAT Scope", $"DeclinationRate Get => {declination:0.00}");
				return declination;
			}
			set
			{
				LogMessage("OAT Scope", "DeclinationRate Set - Not implemented");
				throw new ASCOM.PropertyNotImplementedException("DeclinationRate", true);
			}
		}

		public PierSide DestinationSideOfPier(double RightAscension, double Declination)
		{
			LogMessage("OAT Scope", "DestinationSideOfPier Get - Not implemented");
			throw new ASCOM.MethodNotImplementedException("DestinationSideOfPier");
		}

		public bool DoesRefraction
		{
			get
			{
				LogMessage("OAT Scope", "DoesRefraction Get - Not implemented");
				throw new ASCOM.PropertyNotImplementedException("DoesRefraction", false);
			}
			set
			{
				LogMessage("OAT Scope", "DoesRefraction Set - Not implemented");
				throw new ASCOM.PropertyNotImplementedException("DoesRefraction", true);
			}
		}

		public EquatorialCoordinateType EquatorialSystem
		{
			// TODO : Determine if we're using JNow or J2000, or can use both.  Work on this.
			get
			{
				EquatorialCoordinateType equatorialSystem = EquatorialCoordinateType.equTopocentric;
				LogMessage("OAT Scope", $"DeclinationRate Get => Topocentric");
				return equatorialSystem;
			}
		}

		public void FindHome()
		{
			LogMessage("OAT Scope", "FindHome - Not implemented");
			throw new ASCOM.MethodNotImplementedException("FindHome");
		}

		public double FocalLength
		{
			get
			{
				LogMessage("OAT Scope", "FocalLength Get - Not implemented");
				throw new ASCOM.PropertyNotImplementedException("FocalLength", false);
			}
		}

		public double GuideRateDeclination
		{
			get
			{
				LogMessage("OAT Scope", "GuideRateDeclination Get - Not implemented");
				throw new ASCOM.PropertyNotImplementedException("GuideRateDeclination", false);
			}
			set
			{
				LogMessage("OAT Scope", "GuideRateDeclination Set - Not implemented");
				throw new ASCOM.PropertyNotImplementedException("GuideRateDeclination", true);
			}
		}

		public double GuideRateRightAscension
		{
			get
			{
				LogMessage("OAT Scope", "GuideRateRightAscension Get - Not implemented");
				throw new ASCOM.PropertyNotImplementedException("GuideRateRightAscension", false);
			}
			set
			{
				LogMessage("OAT Scope", "GuideRateRightAscension Set - Not implemented");
				throw new ASCOM.PropertyNotImplementedException("GuideRateRightAscension", true);
			}
		}

		public bool IsPulseGuiding
		{
			get
			{
				bool retVal = Convert.ToBoolean(System.Convert.ToInt32(CommandString(":GIG#,#")));
				LogMessage("OAT Scope", "IsPulseGuiding Get => " + retVal.ToString());
				return retVal;
			}
		}


		private bool _trackingPriorToMove;
		private double _ratePriorToMove;
		public void MoveAxis(TelescopeAxes Axis, double Rate)
		{
			LogMessage("OAT Scope", $"MoveAxis({Axis}, {Rate:0.00})");

			if (Axis == TelescopeAxes.axisTertiary)
			{
				LogMessage("OAT Scope", "MoveAxis - Axis not supported.");
				throw new ASCOM.NotImplementedException("MoveAxis Tertiary Not Supported.");
			}

			if (AtPark)
			{
				LogMessage("OAT Scope", "MoveAxis - Scope is parked");
				throw new ASCOM.ParkedException("MoveAxis");
			}

			if (Rate != 0 && !ValidAxisSpeed(Axis, Rate))
			{
				LogMessage("OAT Scope", $"MoveAxis - invalid speed {Rate:0.000} for axis {Axis}");
				throw new ASCOM.InvalidValueException("Invalid speed for Axis");
			}

			var sAxis = Enum.GetName(typeof(TelescopeAxes), Axis);
			string cmd = "Q";


			if (Rate == 0)
			{
				LogMessage("OAT Scope", $"MoveAxis - {sAxis} Rate is zero, so stopping Slew and setting Rate S");
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
				LogMessage("OAT Scope", $"MoveAxis - {sAxis} Finding Rate for {rate}");
				foreach (Rate availRate in AxisRates(Axis))
				{
					LogMessage("OAT Scope", $"MoveAxis - {sAxis} Check rate {index} : {availRate.Minimum} -> {availRate.Maximum}");

					if (rate < availRate.Maximum)
					{
						cmd = rateCommandParam.Substring(index, 1);
						LogMessage("OAT Scope", $"MoveAxis - {sAxis} Rate found. Using mode {cmd}");
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
				CommandBlind(":hP");
				PollUntilZero(":GIS#,#");
				_isParked = true;
				LogMessage("OAT Scope", "Park - Parked mount");
			}
		}

		public void PulseGuide(GuideDirections Direction, int Duration)
		{
			if (!AtPark)
			{
				var dirString = Enum.GetName(typeof(GuideDirections), Direction);
				var durString = Duration.ToString("0000");
				LogMessage("OAT Scope", $"PulseGuide ({Direction}, {durString}ms)");
				var dir = dirString.Substring(5, 1).ToLower();
				CommandBlind($":Mg{dir}{durString}");
			}
			else
			{
				LogMessage("OAT Scope", "PulseGuide - Scope is parked");
				throw new ASCOM.ParkedException("PulseGuide");
			}
		}

		public double RightAscension
		{
			get
			{
				double rightAscension = 0.0;
				rightAscension = _utilities.HMSToHours(CommandString(":GR#,#"));
				LogMessage("OAT Scope", $"RightAscension Get => {rightAscension:0.000}");
				return rightAscension;
			}
		}

		public double RightAscensionRate
		{
			get
			{
				double rightAscensionRate = 0.0;
				LogMessage("OAT Scope", $"RightAscensionRate Get => {rightAscensionRate:0.000}");
				return rightAscensionRate;
			}
			set
			{
				LogMessage("OAT Scope", "RightAscensionRate Set - Not implemented");
				throw new ASCOM.PropertyNotImplementedException("RightAscensionRate", true);
			}
		}

		public void SetPark()
		{
			LogMessage("OAT Scope", "SetPark - Not implemented");
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

				LogMessage("OAT Scope", $"SideOfPier Get => {Enum.GetName(typeof(PierSide), retVal)}");
				return retVal;
			}
			set
			{
				LogMessage("OAT Scope", "SideOfPier Set - Not Implemented");
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
				LogMessage("OAT Scope", $"SiderealTime Get => {lst:0.000}");
				return lst;
			}
		}

		public double SiteElevation
		{
			get
			{
				LogMessage("OAT Scope", $"SiteElevation Get => {Profile.Elevation:0.0}");
				return Profile.Elevation;
			}
			set
			{
				if (value >= -300 && value <= 10000)
				{
					LogMessage("OAT Scope", $"SiteElevation Set {value:0.0}");
					Profile.Elevation = value;
				}
				else
				{
					LogMessage("OAT Scope", $"SiteElevation Set {value:0.0} out of range, Atlantis or Mt Everest no supported.");
					throw new ASCOM.InvalidValueException("SiteElevation");
				}
			}
		}

		public double SiteLatitude
		{
			get
			{
				LogMessage("OAT Scope", $"SiteLatitude Get => {Profile.Latitude:0.00}");
				return Profile.Latitude;
			}
			set
			{
				LogMessage("OAT Scope", "SiteLatitude Set - Not implemented");
				throw new ASCOM.PropertyNotImplementedException("SiteLatitude", true);
			}
		}

		public double SiteLongitude
		{
			get
			{
				LogMessage("OAT Scope", $"SiteLongitude Get => {Profile.Longitude:0.00}");
				return Profile.Longitude;
			}
			set
			{
				LogMessage("OAT Scope", "SiteLongitude Set - Not implemented");
				throw new ASCOM.PropertyNotImplementedException("SiteLongitude", true);
			}
		}

		public short SlewSettleTime
		{
			get
			{
				LogMessage("OAT Scope", "SlewSettleTime Get - Not implemented");
				throw new ASCOM.PropertyNotImplementedException("SlewSettleTime", false);
			}
			set
			{
				LogMessage("OAT Scope", "SlewSettleTime Set - Not implemented");
				throw new ASCOM.PropertyNotImplementedException("SlewSettleTime", true);
			}
		}

		public void SlewToAltAz(double Azimuth, double Altitude)
		{
			LogMessage("OAT Scope", "SlewToAltAz - Not implemented");
			throw new ASCOM.MethodNotImplementedException("SlewToAltAz");
		}

		public void SlewToAltAzAsync(double Azimuth, double Altitude)
		{
			LogMessage("OAT Scope", "SlewToAltAzAsync - Not implemented");
			throw new ASCOM.MethodNotImplementedException("SlewToAltAzAsync");
		}

		public void SlewToCoordinates(double ra, double dec) =>
			SlewToCoordinatesWithWait("SlewToCoordinates", ra, dec, true);

		private void SlewToCoordinatesWithWait(string caller, double ra, double dec, bool wait)
		{
			if (AtPark)
			{
				LogMessage("OAT Scope", $"{caller} - Scope is parked!");
				throw new ASCOM.ParkedException(caller);
			}

			if (ValidateCoordinates(ra, dec))
			{
				LogMessage("OAT Scope", $"{caller} - RA {ra:0.000},  Dec {dec:0.000}");
				SetTargetCoordinates(ra, dec);
				CommandString(":MS#,n");
				if (wait)
				{
					PollUntilZero(":GIS#,#");
				}
			}
			else
			{
				LogMessage("OAT Scope", $"{caller} - Invalid coordinates RA: " + ra.ToString() + ", Dec: " + dec.ToString());
				throw new ASCOM.InvalidValueException(caller);
			}
		}

		private bool SetTargetCoordinates(double rightAscension, double declination)
		{
			var strRAcmd = $":Sr{_utilities.HoursToHMS(rightAscension, ":", ":")}#,n";
			var strDeccmd = $":Sd{DegreesToDmsWithSign(declination, "*", ":", "")}#,n";
			LogMessage("OAT Scope", $"SetTargetCoordinates - RA : {rightAscension:0.000}, DEC: {declination:0.00}");

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
				LogMessage("OAT Scope", $"SlewToTarget - RA: {TargetRightAscension:0.000}, DEC: {TargetDeclination:0.000}");
				SlewToCoordinates(TargetRightAscension, TargetDeclination);
			}
			else
			{
				LogMessage("OAT Scope", $"SlewToTarget - No Target RA, DEC set");
				throw new InvalidValueException("TargetRightAscension or TargetDeclination are not set or valid");
			}
		}

		public void SlewToTargetAsync()
		{
			if (_targetRaSet && _targetDecSet)
			{
				LogMessage("OAT Scope", $"SlewToTargetAsync - RA: {TargetRightAscension:0.000}, DEC: {TargetDeclination:0.000}");
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
				bool retVal = Convert.ToBoolean(System.Convert.ToInt32(CommandString(":GIS#,#")));
				LogMessage("OAT Scope", "Slewing Get => " + retVal.ToString());
				return retVal;
			}
		}

		public void SyncToAltAz(double Azimuth, double Altitude)
		{
			LogMessage("OAT Scope", "SyncToAltAz - Not implemented");
			throw new ASCOM.MethodNotImplementedException("SyncToAltAz");
		}

		public void SyncToCoordinates(double RightAscension, double Declination)
		{
			if (AtPark)
			{
				LogMessage("OAT Scope", "SyncToCoordinates - Scope is parked!");
				throw new ASCOM.ParkedException("SyncToCoordinates");
			}

			if (ValidateCoordinates(RightAscension, Declination))
			{
				LogMessage("OAT Scope", $"SyncToCoordinates - RA: {TargetRightAscension:0.000}, DEC: {TargetDeclination:0.000}");
				SetTargetCoordinates(RightAscension, Declination);
				CommandString(":CM#,#");
			}
			else
			{
				LogMessage("OAT Scope", "SyncToCoordinates - Invalid coordinates RA: " + RightAscension.ToString() + ", Dec: " + Declination.ToString());
				throw new ASCOM.InvalidValueException("SyncToCoordinates");
			}
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
					LogMessage("OAT Scope", $"TargetDeclination Get => {_targetDec:0.000}");
					return _targetDec;
				}
				else
				{
					LogMessage("OAT Scope", "TargetDeclination Get => Value not set");
					throw new ASCOM.ValueNotSetException("TargetDeclination");
				}
			}
			set
			{
				if (value >= -90 && value <= 90)
				{
					LogMessage("OAT Scope", $"TargetDeclination Set => {value:0.000}");
					_targetDec = value;
					_targetDecSet = true;
				}
				else
				{
					LogMessage("OAT Scope", $"TargetDeclination Set - Invalid Value {value:0.000}");
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
					LogMessage("OAT Scope", $"TargetRightAscension Get => {_targetRa:0.000}");
					return _targetRa;
				}
				else
				{
					LogMessage("OAT Scope", "TargetRightAscension Get => Value not set");
					throw new ASCOM.ValueNotSetException("TargetRightAscension");
				}
			}
			set
			{
				if (value >= 0 && value <= 24)
				{
					LogMessage("OAT Scope", $"TargetRightAscension Set - {value:0.000}");
					_targetRa = value;
					_targetRaSet = true;
				}
				else
				{
					LogMessage("OAT Scope", $"TargetRightAscension Set - Invalid Value {value:0.000}");
					throw new ASCOM.InvalidValueException("TargetRightAscension");
				}
			}
		}

		public bool Tracking
		{
			get
			{
				if (CommandString(":GIT#,#") == "0")
				{
					_isTracking = false;
					LogMessage("OAT Scope", "Tracking Get => false");
				}
				else
				{
					_isTracking = true;
					LogMessage("OAT Scope", "Tracking Get => true");
				}

				return _isTracking;
			}
			set
			{
				if (CommandString($":MT{Convert.ToInt32(value)}#,n") == "1")
				{
					_isTracking = value;
					LogMessage("OAT Scope", $"Tracking Set - {value}");
				}
				else
				{
					LogMessage("OAT Scope", "Tracking Set - Error, OAT returned non-1 for :MTx# command");
					throw new ASCOM.DriverException("Error setting tracking state");
				}
			}
		}

		public DriveRates TrackingRate
		{
			get
			{
				LogMessage("OAT Scope", $"TrackingRate Get => {Enum.GetName(typeof(DriveRates), driveRate)}");
				return driveRate;
			}
			set
			{
				LogMessage("OAT Scope", $"TrackingRate Set - Ignoring value {value}. Only sidereal supported.");
				driveRate = DriveRates.driveSidereal;
			}
		}

		public ITrackingRates TrackingRates
		{
			get
			{
				ITrackingRates trackingRates = new TrackingRates();
				string rates = string.Join(", ", trackingRates.ToString());
				LogMessage("OAT Scope", "TrackingRates Get => " + rates);
				return trackingRates;
			}
		}

		public DateTime UTCDate
		{
			// ToDo - Can we handle this without bothering the mount?
			get
			{
				DateTime utcDate = DateTime.UtcNow;
				LogMessage("OAT Scope", $"UTCDate Get => {utcDate}");
				return utcDate;
			}
			set { throw new ASCOM.PropertyNotImplementedException("UTCDate", true); }
		}

		public void Unpark()
		{
			if (AtPark)
			{
				string unprkRet = CommandString(":hU#,n");
				if (unprkRet == "1")
				{
					LogMessage("OAT Scope", "Unpark - Unparked mount");
				}
				_isParked = false;
			}
		}

		private string DegreesToDmsWithSign(double degrees, string degSep = "°", string minSep = "'", string secondSep = "\"")
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

		private void LogMessage(string identifier, string message)
		{
			Debug.WriteLine($"{identifier} - {message}");
			_tl.LogMessage(identifier, message);
		}
	}
}
