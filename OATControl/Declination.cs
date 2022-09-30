using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OATControl
{

	//////////////////////////////////////////////////////////////////////////////////////
	//
	// Declination 
	//
	// A class to handle degrees, minutes, seconds of a declination

	// Parses the RA or DEC from a string that has an optional sign, a two digit degree, a seperator, a two digit minute, a seperator and a two digit second.
	// For example:   -45*32:11 or 23:44:22

	// In the northern hemisphere, 0 is north pole, -180 is south pole
	// In the southern hemisphere, 0 is south pole, -180 is north pole
	public class Declination : DayTime
	{
		const long ArcSecondsPerHemisphere = 180L * 60L * 60L;  // Arc-seconds in 180 degrees

		public Declination() : base()
		{
		}

		public Declination(Declination other) : base(other)
		{
		}

		public Declination(int h, int m, int s) : base(h, m, s)
		{
		}

		public Declination(float inDegrees) : base(inDegrees)
		{
		}

		// Change the degree component
		public bool ChangeDegree(int newDegree)
		{
			long tsecs = _totalSeconds;
			int hours, mins, secs;
			GetTime(out hours, out mins, out secs);
			var newDec = new Declination(newDegree, mins, secs);
			if ((newDegree == 0) && (_totalSeconds != 0))
			{
				_totalSeconds = Math.Sign(_totalSeconds) * newDec._totalSeconds;
			}
			else
			{
				_totalSeconds = newDec._totalSeconds;
			}
			CheckHours();
			return tsecs != _totalSeconds;
		}

		// Change the minute component only
		public override bool ChangeMinute(int newMins)
		{
			long tsecs = _totalSeconds;
			int hours, mins, secs;
			GetTime(out hours, out mins, out secs);
			var newTime = new Declination(hours, newMins, secs);
			_totalSeconds = newTime._totalSeconds;
			CheckHours();
			return tsecs != _totalSeconds;
		}

		// Change the seconds component only
		public override bool ChangeSecond(int newSecs)
		{
			long tsecs = _totalSeconds;
			int hours, mins, secs;
			GetTime(out hours, out mins, out secs);
			var newTime = new Declination(hours, mins, newSecs);
			_totalSeconds = newTime._totalSeconds;
			CheckHours();
			return tsecs != _totalSeconds;
		}

		public int Degrees
		{
			get {
				return Hours;
			}
		}

		public void AddDegrees(int deltaDegrees)
		{
			AddHours(deltaDegrees);
		}

		public float TotalDegrees
		{
			get {
				return TotalHours;
			}
		}

		public override void CheckHours()
		{
			if (_totalSeconds > 90 * 3600)
			{
				// LOGV1(DEBUG_GENERAL, F("CheckHours: Degrees is more than 0, clamping"));
				_totalSeconds = 90 * 3600;
			}
			if (_totalSeconds < -90 * 3600)
			{
				// LOGV1(DEBUG_GENERAL, F("CheckHours: Degrees is less than -180, clamping"));
				_totalSeconds = -90 * 3600;
			}
		}

		// Convert to a standard string (like 14:45:06), specifying separators if needed
		//public string ToDisplayString(char sep1, char sep2)
		//{
		//	string s;
		//	s = "{d}" + sep1 + "{m}" + sep2 + "{s}";
		//	return formatString(s);
		//}

		public override string ToString()
		{
			//string s = ToDisplayString('*', ':') + string.Format(" ({0:0.000})", TotalHours);
			int degrees, mins, secs;
			GetTime(out degrees, out mins, out secs);

			int absDegrees = Math.Abs(degrees);
			string sign = _totalSeconds < 0 ? "-" : "";
			string p = $"{sign}{absDegrees:00}*{mins:00}:{secs:00} ({TotalDegrees:0.0000})";

			return p;

			//return s;
		}

		public static new Declination ParseFromMeade(String s)
		{
			Declination result = new Declination();
			// LOGV2(DEBUG_GENERAL, F("Declination.Parse(%s)"), s.c_str());

			// Use the DayTime code to parse it...
			DayTime dt = DayTime.ParseFromMeade(s);

			// ...and then correct for hemisphere
			//result._totalSeconds = dt.TotalSeconds + (NORTHERN_HEMISPHERE? -(ArcSecondsPerHemisphere / 2) : (ArcSecondsPerHemisphere / 2));
			//LOGV3(DEBUG_GENERAL, F("Declination.Parse(%s) -> %s"), s.c_str(), result.ToString());
			result._totalSeconds = dt.TotalSeconds;
			return result;
		}

		public static Declination FromSeconds(long secs)
		{
			var dec = new Declination();
			dec._totalSeconds = secs;
			return dec;
		}
		//
		//	Declination public FromSeconds(long seconds)
		//	{
		//		seconds += (NORTHERN_HEMISPHERE ? -(ArcSecondsPerHemisphere / 2) : (ArcSecondsPerHemisphere / 2));
		//		return Declination(1.0 * seconds / 3600.0);
		//	}
		//
		//	const char* public formatString(char* targetBuffer, const char* format, long*) const
		//	{
		//  long secs = totalSeconds;
		//	secs = NORTHERN_HEMISPHERE? (secs + ArcSecondsPerHemisphere / 2) : (secs - arcSecondsPerHemisphere / 2);
		//return DayTime::formatString(targetBuffer, format, &secs);
	}
}
