using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using OATCommunications.CommunicationHandlers;
using OATCommunications.Model;
using OATCommunications.TelescopeCommandHandlers;
using OATCommunications.Utilities;

namespace OATCommunications
{
	public class OatmealTelescopeCommandHandlers : ITelescopeCommandHandler
	{
		private readonly ICommunicationHandler _commHandler;

		private int _moveState = 0;

		public MountState MountState { get; } = new MountState();

		public OatmealTelescopeCommandHandlers(ICommunicationHandler commHandler)
		{
			_commHandler = commHandler;
		}

		public bool Connected { get { return _commHandler.Connected; } }

		public async Task<bool> RefreshMountState()
		{
			AsyncAutoResetEvent doneEvent = new AsyncAutoResetEvent();
			var _slewingStates = new[] { "SlewToTarget", "FreeSlew", "ManualSlew" };
			bool success = false;
			SendCommand(":GX#,#", (status) =>
			{
				if (status.Success)
				{
					var parts = status.Data.Split(',');
					MountState.IsTracking = parts[1][2] == 'T';
					MountState.IsSlewing = _slewingStates.Contains(parts[0]);
					MountState.RightAscension = GetCompactRA(parts[5]);
					MountState.Declination = GetCompactDec(parts[6]);
					success = true;
				}
			});

			await doneEvent.WaitAsync();
			return success;

		}

		private double GetCompactDec(string part)
		{
			var d = int.Parse(part.Substring(0, 3));
			var m = int.Parse(part.Substring(3, 2));
			var s = int.Parse(part.Substring(5, 2));

			return d + m / 60.0 + s / 3600.0;
		}

		private double GetCompactRA(string part)
		{
			var h = int.Parse(part.Substring(0, 2));
			var m = int.Parse(part.Substring(2, 2));
			var s = int.Parse(part.Substring(4, 2));

			return h + m / 60.0 + s / 3600.0;
		}

		public async Task<TelescopePosition> GetPosition()
		{
			AsyncAutoResetEvent doneBoth = new AsyncAutoResetEvent();
			bool error = false;
			SendCommand(":GR#,#", (ra) =>
			{
				if (ra.Success && TryParseRA(ra.Data, out double dRa))
				{
					MountState.RightAscension = dRa;
				}
				else
				{
					error = true;
				}
			});

			SendCommand(":GD#,#", (dec) =>
			{
				if (dec.Success && TryParseDec(dec.Data, out double dDec))
				{
					MountState.Declination = dDec;
				}
				else
				{
					error = true;
				}
				doneBoth.Set();
			});

			await doneBoth.WaitAsync();

			return error ? TelescopePosition.Invalid : new TelescopePosition(MountState.RightAscension, MountState.Declination, Epoch.JNOW);
		}

		public async Task<float> GetSiteLatitude()
		{
			AsyncAutoResetEvent doneEvent = new AsyncAutoResetEvent();
			double latitude = 0;
			bool success = false;

			SendCommand(":Gt#,#", (lat) =>
			{
				if (lat.Success && TryParseDec(lat.Data, out latitude))
				{
					success = true;
				}
				doneEvent.Set();
			});

			await doneEvent.WaitAsync();

			return success ? (float)latitude : 0;
		}

		public async Task<float> GetSiteLongitude()
		{
			AsyncAutoResetEvent doneEvent = new AsyncAutoResetEvent();
			double longitude = 0;
			bool success = false;

			SendCommand(":Gg#,#", (lat) =>
			{
				if (lat.Success && TryParseDec(lat.Data, out longitude))
				{
					success = true;
					longitude = 180 - longitude;
				}
				doneEvent.Set();
			});

			await doneEvent.WaitAsync();

			return success ? (float)longitude : 0;
		}

		public async Task<string> SetSiteLatitude(float latitude)
		{
			AsyncAutoResetEvent doneEvent = new AsyncAutoResetEvent();
			bool success = false;
			char sgn = latitude < 0 ? '-' : '+';
			int latInt = (int)Math.Abs(latitude);
			int latMin = (int)((Math.Abs(latitude) - latInt) * 60.0f);
			SendCommand($":St{sgn}{latInt:00}*{latMin:00}#,n", (result) =>
			{
				success = result.Success;
				doneEvent.Set();
			});

			await doneEvent.WaitAsync();
			return success ? "1" : "0";
		}

		// Input is -180 (W) to +180 (E)
		// This needs to be mapped to 360..0
		public async Task<string> SetSiteLongitude(float longitude)
		{
			AsyncAutoResetEvent doneEvent = new AsyncAutoResetEvent();
			bool success = false;
			longitude = 180 - longitude;
			int lonInt = (int)longitude;
			int lonMin = (int)((longitude - lonInt) * 60.0f);
			SendCommand($":Sg{lonInt:000}*{lonMin:00}#,n", (result) =>
			{
				success = result.Success;
				doneEvent.Set();
			});

			await doneEvent.WaitAsync();
			return success ? "1" : "0";
		}

		private void FloatToHMS(double val, out int h, out int m, out int s)
		{
			h = (int)Math.Floor(val);
			val = (val - h) * 60;
			m = (int)Math.Floor(val);
			val = (val - m) * 60;
			s = (int)Math.Round(val);
		}

		private bool TryParseRA(string ra, out double dRa)
		{
			try
			{
				var parts = ra.Split(':');
				dRa = int.Parse(parts[0]) + int.Parse(parts[1]) / 60.0 + int.Parse(parts[2]) / 3600.0;
				return true;
			}
			catch (Exception ex)
			{
				Log.WriteLine("OAT: Can't parse RA from {0}", ra);
			}
			dRa = 0;
			return false;
		}

		private bool TryParseDec(string dec, out double dDec)
		{
			try
			{
				var parts = dec.Split('*', '\'');
				dDec = int.Parse(parts[0]) + int.Parse(parts[1]) / 60.0;
				if (parts.Length > 2)
				{
					dDec += int.Parse(parts[2]) / 3600.0;
				}

				return true;
			}
			catch (Exception ex)
			{
				Log.WriteLine("OAT: Can't parse DEC from {0}", dec);
			}
			dDec = 0;
			return false;
		}

		public async Task<bool> StartMoving(string dir)
		{
			AsyncAutoResetEvent doneEvent = new AsyncAutoResetEvent();
			bool success = false;
			SendCommand($":M{dir}#", (a) =>
			{
				MountState.IsSlewing = true;
				++_moveState;
				success = a.Success;
				doneEvent.Set();
			});

			await doneEvent.WaitAsync();
			return success;
		}

		public async Task<bool> StopMoving(string dir)
		{
			AsyncAutoResetEvent doneEvent = new AsyncAutoResetEvent();
			bool success = false;
			SendCommand($":Q{dir}#", (a) =>
			{
				--_moveState;
				if (_moveState <= 0)
				{
					_moveState = 0;
					MountState.IsSlewing = false;
				}
				success = a.Success;
				doneEvent.Set();
			});
			await doneEvent.WaitAsync();
			return success;
		}

		public async Task<bool> Slew(TelescopePosition position)
		{
			int deg, hour, min, sec;
			AsyncAutoResetEvent doneEvent = new AsyncAutoResetEvent();
			bool success = false;

			FloatToHMS(Math.Abs(position.Declination), out deg, out min, out sec);
			string sign = position.Declination < 0 ? "-" : "+";
			SendCommand($":Sd{sign}{deg:00}*{min:00}:{sec:00}#,n", (decResult) =>
			{
				success = decResult.Success && decResult.Data == "1";
			});

			FloatToHMS(Math.Abs(position.RightAscension), out hour, out min, out sec);
			SendCommand($":Sr{hour:00}:{min:00}:{sec:00}#,n", (raResult) =>
			{
				success = success && raResult.Success && raResult.Data == "1";
			});

			SendCommand($":MS#,n", (moveResult) =>
			{
				success = success && moveResult.Success && moveResult.Data == "1";
			});

			await doneEvent.WaitAsync();
			return success;
		}

		public async Task<bool> Sync(TelescopePosition position)
		{
			int deg, hour, min, sec;
			AsyncAutoResetEvent doneEvent = new AsyncAutoResetEvent();
			bool success = false;

			FloatToHMS(Math.Abs(position.Declination), out deg, out min, out sec);
			string sign = position.Declination < 0 ? "-" : "+";
			SendCommand($":Sd{sign}{deg:00}*{min:00}:{sec:00}#,n", (decResult) =>
			{
				success = decResult.Success && decResult.Data == "1";
			});

			FloatToHMS(Math.Abs(position.RightAscension), out hour, out min, out sec);
			SendCommand($":Sr{hour:00}:{min:00}:{sec:00}#,n", (raResult) =>
			{
				success = success && raResult.Success && raResult.Data == "1";
			});

			SendCommand($":CM#,#", (syncResult) =>
			{
				success = success && syncResult.Success && syncResult.Data == "1";
				doneEvent.Set();
			});

			await doneEvent.WaitAsync();
			return success;
		}

		public async Task<bool> GoHome()
		{
			bool success = false;
			AsyncAutoResetEvent doneEvent = new AsyncAutoResetEvent();

			SendCommand($":hP#", (moveResult) =>
			{
				success = moveResult.Success;
				doneEvent.Set();
			});

			await doneEvent.WaitAsync();
			return success;
		}

		public async Task<bool> SetHome()
		{
			bool success = false;
			AsyncAutoResetEvent doneEvent = new AsyncAutoResetEvent();

			SendCommand($":hP#", (moveResult) =>
			{
				success = moveResult.Success;
				doneEvent.Set();
			});

			await doneEvent.WaitAsync();
			return success;
		}

		public async Task<bool> SetTracking(bool enabled)
		{
			bool success = false;
			AsyncAutoResetEvent doneEvent = new AsyncAutoResetEvent();
			var b = enabled ? 1 : 0;
			SendCommand($":MT{b}#,n", (setResult) =>
			{
				if (setResult.Success)
				{
					MountState.IsTracking = enabled;
				}
				doneEvent.Set();
			});

			await doneEvent.WaitAsync();
			return success;
		}

		public async Task<bool> SetLocation(double lat, double lon, double altitudeInMeters)
		{
			bool success = false;
			AsyncAutoResetEvent doneEvent = new AsyncAutoResetEvent();

			// Longitude
			lon = 180 - lon;

			int lonFront = (int)lon;
			int lonBack = (int)((lon - lonFront) * 60);
			var lonCmd = $":Sg{lonFront:000}*{lonBack:00}#,n";
			SendCommand(lonCmd, (status) => success = status.Success);

			// Latitude
			var latSign = lat > 0 ? '+' : '-';
			var absLat = Math.Abs(lat);
			int latFront = (int)absLat;
			int latBack = (int)((absLat - latFront) * 60.0);
			var latCmd = $":St{latSign}{latFront:00}*{latBack:00}#,n";
			SendCommand(latCmd, (status) => success = success && status.Success);

			// GMT Offset
			var offsetSign = DateTimeOffset.Now.Offset.TotalHours > 0 ? "+" : "-";
			var offset = Math.Abs(DateTimeOffset.Now.Offset.TotalHours);
			SendCommand($":SG{offsetSign}{offset:00}#,n", (status) => success = success && status.Success);

			// Local Time and Date
			var n = DateTime.Now;
			SendCommand($":SL{n:HH:mm:ss}#,n", (status) => success = success && status.Success);
			SendCommand($":SC{n:MM/dd/yy}#,#", (status) => { success = success && status.Success; doneEvent.Set(); });

			await doneEvent.WaitAsync();
			return success;
		}

		public void SendCommand(string cmd, Action<CommandResponse> onFulFilled)
		{
			if (!cmd.StartsWith(":"))
			{
				cmd = $":{cmd}";
			}

			if (cmd.EndsWith("#,##"))
			{
				_commHandler.SendCommandDoubleResponse(cmd.Substring(0, cmd.Length - 3), onFulFilled);
			}
			else if (cmd.EndsWith("#,#"))
			{
				_commHandler.SendCommand(cmd.Substring(0, cmd.Length - 2), onFulFilled);
			}
			else if (cmd.EndsWith("#,n"))
			{
				_commHandler.SendCommandConfirm(cmd.Substring(0, cmd.Length - 2), onFulFilled);
			}
			else
			{
				if (!cmd.EndsWith("#"))
				{
					cmd += "#";
				}
				_commHandler.SendBlind(cmd, onFulFilled);
			}
		}
	}
}
