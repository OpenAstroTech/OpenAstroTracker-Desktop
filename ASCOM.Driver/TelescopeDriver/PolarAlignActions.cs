using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ASCOM.OpenAstroTracker
{
	/// <summary>
	/// Implements the PolarAlign Actions contract (Tier 1, spec v0.1, https://wiki.openastrotech.com/en/Development/pa-actions)
	/// on top of the AZ/ALT Meade commands, so a host such as N.I.N.A.'s Three Point Polar Alignment can drive the
	/// AutoPA motors through ASCOM Action() instead of a second connection.
	///
	/// Amounts are arcminutes of polar axis movement: +az increases the azimuth of the polar axis, +alt raises it.
	/// </summary>
	internal class PolarAlignActions
	{
		public const string InfoAction = "PolarAlign:Info";
		public const string MoveAction = "PolarAlign:Move";
		public const string StatusAction = "PolarAlign:Status";
		public const string StopAction = "PolarAlign:Stop";
		public const string HomeAction = "PolarAlign:Home";

		public static readonly string[] Names = { InfoAction, MoveAction, StatusAction, StopAction, HomeAction };

		private const string SpecVersion = "0.1";

		// Largest single move accepted, in arcminutes. Hosts split larger corrections.
		private const double MaxMoveArcmin = 120.0;

		// Suggested wait after a move before the host exposes.
		private const double SettleSeconds = 2.0;

		// First firmware that knows :QZ# (stop AZ/ALT only, leaving tracking on), as major*10000 + minor*100 + patch.
		// TODO: 11323 (V1.13.23) is a placeholder; set it to the firmware release that ships :QZ# once that is known.
		private const long StopAzAltFirmwareVersion = 11323;

		// OAT's own button convention (OATControl, setup dialog): :MAZ+ moves the axis left as seen from behind the
		// mount, i.e. it decreases the azimuth of the polar axis in both hemispheres; :MAL+ raises it.
		// A motor that runs the wrong way is fixed in the firmware configuration, which fixes the buttons too.
		private const int AzCommandSign = -1;
		private const int AltCommandSign = 1;

		private readonly Func<string, string> _send;
		private readonly Func<long> _firmwareVersion;
		private readonly Func<bool> _isParked;
		private readonly Action<string> _log;

		public PolarAlignActions(Func<string, string> send, Func<long> firmwareVersion, Func<bool> isParked, Action<string> log)
		{
			_send = send;
			_firmwareVersion = firmwareVersion;
			_isParked = isParked;
			_log = log;
		}

		/// <summary>The contract's spelling of an action name, or null when it is not one of ours. ASCOM action names are case-insensitive.</summary>
		public static string CanonicalName(string actionName)
		{
			foreach (var name in Names)
			{
				if (string.Equals(name, actionName, StringComparison.OrdinalIgnoreCase))
					return name;
			}
			return null;
		}

		public string Execute(string actionName, string actionParameters)
		{
			switch (CanonicalName(actionName))
			{
				case InfoAction:
					return Info();
				case MoveAction:
					return Move(Json.ParseObject(actionParameters));
				case StatusAction:
					return Status();
				case StopAction:
					return Stop();
				case HomeAction:
					return Home();
				default:
					throw new ASCOM.ActionNotImplementedException("Action " + actionName + " is not supported by this driver");
			}
		}

		private string Info()
		{
			var axes = ReadAxes();
			var json = new StringBuilder();
			json.Append("{\"specVersion\":").Append(Json.Quote(SpecVersion));
			json.Append(",\"tiers\":[1]");
			json.Append(",\"deviceName\":").Append(Json.Quote(_send(":GVP#,#")));
			json.Append(",\"deviceVersion\":").Append(Json.Quote(_send(":GVN#,#")));
			json.Append(",\"axes\":{");
			var first = true;
			foreach (var axis in new[] { "az", "alt" })
			{
				if (!axes.Contains(axis))
					continue;
				if (!first)
					json.Append(',');
				first = false;
				json.Append(Json.Quote(axis)).Append(":{\"maxMove\":").Append(Json.Number(MaxMoveArcmin)).Append(",\"backlashHandled\":false}");
			}
			json.Append('}');
			json.Append(",\"simultaneousMoves\":true");
			json.Append(",\"movesWhileTracking\":true");
			json.Append(",\"movesWhileSlewing\":false");
			json.Append(",\"canStop\":").Append(CanStop ? "true" : "false");
			json.Append(",\"settleSeconds\":").Append(Json.Number(SettleSeconds));
			json.Append('}');
			return json.ToString();
		}

		private string Move(Dictionary<string, object> parameters)
		{
			var axis = (parameters.TryGetValue("axis", out var a) ? a as string : null)?.ToLowerInvariant();
			var amount = parameters.TryGetValue("arcmin", out var m) && m is double d ? d : double.NaN;
			if ((axis != "az" && axis != "alt") || double.IsNaN(amount) || double.IsInfinity(amount))
				throw new ASCOM.InvalidValueException("PolarAlign:Move expects {\"axis\":\"az\"|\"alt\",\"arcmin\":<number>}");
			if (!ReadAxes().Contains(axis))
				throw new ASCOM.InvalidValueException($"PolarAlign:Move: this mount has no motorized {axis} axis");
			if (Math.Abs(amount) > MaxMoveArcmin)
				throw new ASCOM.InvalidValueException($"PolarAlign:Move: {amount.ToString(CultureInfo.InvariantCulture)} arcmin is larger than the {MaxMoveArcmin.ToString(CultureInfo.InvariantCulture)} arcmin limit");
			if (_isParked())
				throw new ASCOM.InvalidOperationException("PolarAlign:Move: the mount is parked");

			var motion = ReadMotion();
			if (motion.SlewingRaOrDec)
				throw new ASCOM.InvalidOperationException("PolarAlign:Move: the mount is slewing");
			if (axis == "az" ? motion.AzMoving : motion.AltMoving)
				throw new ASCOM.InvalidOperationException($"PolarAlign:Move: the {axis} axis is already moving");

			var commanded = Math.Round(amount, 4);
			if (commanded != 0)
			{
				var command = axis == "az"
					? string.Format(CultureInfo.InvariantCulture, ":MAZ{0:+0.0000;-0.0000}#", AzCommandSign * commanded)
					: string.Format(CultureInfo.InvariantCulture, ":MAL{0:+0.0000;-0.0000}#", AltCommandSign * commanded);
				_log($"PolarAlign:Move {axis} {commanded.ToString(CultureInfo.InvariantCulture)}' -> {command}");
				_send(command);
			}
			return "{\"arcmin\":" + Json.Number(commanded) + "}";
		}

		private string Status()
		{
			var axes = ReadAxes();
			var motion = ReadMotion();
			var parts = new List<string>();
			if (axes.Contains("az"))
				parts.Add("\"az\":{\"moving\":" + (motion.AzMoving ? "true" : "false") + "}");
			if (axes.Contains("alt"))
				parts.Add("\"alt\":{\"moving\":" + (motion.AltMoving ? "true" : "false") + "}");
			return "{" + string.Join(",", parts.ToArray()) + "}";
		}

		private string Stop()
		{
			if (!CanStop)
				throw new ASCOM.InvalidOperationException("PolarAlign:Stop needs a firmware that supports :QZ#; Info reports canStop:false");
			// :QZ# stops both axes; OAT cannot stop one of them on its own, so an axis parameter stops both.
			_send(":QZ#");
			return string.Empty;
		}

		private string Home()
		{
			_send(":MAAH#,n");
			return string.Empty;
		}

		private bool CanStop => _firmwareVersion() >= StopAzAltFirmwareVersion;

		/// <summary>The motorized axes ("az", "alt"), from the AzAlt field of :XGM#.</summary>
		private HashSet<string> ReadAxes()
		{
			var axes = new HashSet<string>();
			foreach (var field in _send(":XGM#,#").Split(','))
			{
				switch (field.Trim())
				{
					case "AUTO_AZ_ALT":
						axes.Add("az");
						axes.Add("alt");
						break;
					case "AUTO_AZ":
						axes.Add("az");
						break;
					case "AUTO_ALT":
						axes.Add("alt");
						break;
				}
			}
			return axes;
		}

		private struct Motion
		{
			public bool SlewingRaOrDec;
			public bool AzMoving;
			public bool AltMoving;
		}

		/// <summary>Reads the motion-state field of :GX# ("RrDdTZzAaFf", '-' = stopped).</summary>
		private Motion ReadMotion()
		{
			var reply = _send(":GX#,#");
			var fields = reply.Split(',');
			if (fields.Length < 2 || fields[1].Length < 5)
				throw new ASCOM.DriverException($"Unexpected :GX# reply '{reply}'");
			var state = fields[1];
			return new Motion
			{
				SlewingRaOrDec = state[0] != '-' || state[1] != '-',
				AzMoving = state[3] != '-',
				AltMoving = state[4] != '-',
			};
		}

		/// <summary>
		/// Just enough JSON for the contract's flat parameter objects, since the driver targets .NET 4.0 Client Profile.
		/// Values are strings, numbers (as double), booleans or null; nested objects and arrays are skipped.
		/// </summary>
		internal static class Json
		{
			public static string Quote(string text)
			{
				var sb = new StringBuilder("\"");
				foreach (var c in text ?? string.Empty)
				{
					switch (c)
					{
						case '"': sb.Append("\\\""); break;
						case '\\': sb.Append("\\\\"); break;
						case '\n': sb.Append("\\n"); break;
						case '\r': sb.Append("\\r"); break;
						case '\t': sb.Append("\\t"); break;
						default:
							if (c < ' ')
								sb.Append("\\u").Append(((int)c).ToString("x4"));
							else
								sb.Append(c);
							break;
					}
				}
				return sb.Append('"').ToString();
			}

			public static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);

			public static Dictionary<string, object> ParseObject(string text)
			{
				var result = new Dictionary<string, object>(StringComparer.Ordinal);
				if (string.IsNullOrWhiteSpace(text))
					return result;
				var pos = 0;
				SkipWhitespace(text, ref pos);
				Expect(text, ref pos, '{');
				SkipWhitespace(text, ref pos);
				if (Peek(text, pos) == '}')
					return result;
				while (true)
				{
					SkipWhitespace(text, ref pos);
					var key = ParseString(text, ref pos);
					SkipWhitespace(text, ref pos);
					Expect(text, ref pos, ':');
					SkipWhitespace(text, ref pos);
					result[key] = ParseValue(text, ref pos);
					SkipWhitespace(text, ref pos);
					if (Peek(text, pos) == ',')
					{
						pos++;
						continue;
					}
					Expect(text, ref pos, '}');
					SkipWhitespace(text, ref pos);
					if (pos != text.Length)
						throw Malformed(text);
					return result;
				}
			}

			private static object ParseValue(string text, ref int pos)
			{
				var c = Peek(text, pos);
				if (c == '"')
					return ParseString(text, ref pos);
				if (c == '{' || c == '[')
				{
					SkipNested(text, ref pos);
					return null;
				}
				if (Matches(text, ref pos, "true"))
					return true;
				if (Matches(text, ref pos, "false"))
					return false;
				if (Matches(text, ref pos, "null"))
					return null;
				var start = pos;
				while (pos < text.Length && "+-0123456789.eE".IndexOf(text[pos]) >= 0)
					pos++;
				if (!double.TryParse(text.Substring(start, pos - start), NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
					throw Malformed(text);
				return number;
			}

			private static string ParseString(string text, ref int pos)
			{
				Expect(text, ref pos, '"');
				var sb = new StringBuilder();
				while (pos < text.Length)
				{
					var c = text[pos++];
					if (c == '"')
						return sb.ToString();
					if (c != '\\')
					{
						sb.Append(c);
						continue;
					}
					if (pos >= text.Length)
						break;
					var e = text[pos++];
					switch (e)
					{
						case 'n': sb.Append('\n'); break;
						case 'r': sb.Append('\r'); break;
						case 't': sb.Append('\t'); break;
						case 'b': sb.Append('\b'); break;
						case 'f': sb.Append('\f'); break;
						case 'u':
							if (pos + 4 > text.Length)
								throw Malformed(text);
							sb.Append((char)int.Parse(text.Substring(pos, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
							pos += 4;
							break;
						default: sb.Append(e); break;
					}
				}
				throw Malformed(text);
			}

			private static void SkipNested(string text, ref int pos)
			{
				var depth = 0;
				while (pos < text.Length)
				{
					var c = text[pos];
					if (c == '"')
					{
						ParseString(text, ref pos);
						continue;
					}
					pos++;
					if (c == '{' || c == '[')
						depth++;
					else if ((c == '}' || c == ']') && --depth == 0)
						return;
				}
				throw Malformed(text);
			}

			private static bool Matches(string text, ref int pos, string literal)
			{
				if (string.CompareOrdinal(text, pos, literal, 0, literal.Length) != 0)
					return false;
				pos += literal.Length;
				return true;
			}

			private static char Peek(string text, int pos) => pos < text.Length ? text[pos] : '\0';

			private static void Expect(string text, ref int pos, char c)
			{
				if (Peek(text, pos) != c)
					throw Malformed(text);
				pos++;
			}

			private static void SkipWhitespace(string text, ref int pos)
			{
				while (pos < text.Length && char.IsWhiteSpace(text[pos]))
					pos++;
			}

			private static ASCOM.InvalidValueException Malformed(string text) =>
				new ASCOM.InvalidValueException($"Action parameters are not a valid JSON object: {text}");
		}
	}
}
