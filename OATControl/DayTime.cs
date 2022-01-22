using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OATControl
{

	///////////////////////////////////
	// DayTime (and Declination below)
	//
	// A class to handle hours, minutes, seconds in a unified manner, allowing
	// addition of hours, minutes, seconds, other times and conversion to string.

	// Parses the RA or DEC from a string that has an optional Math.Sign, a two digit degree, a seperator, a two digit minute, a seperator and a two digit second.
	// Does not correct for hemisphere (derived class Declination takes care of that)
	// For example:   -45*32:11 or 23:44:22
	public class DayTime
	{
		const long secondsPerDay = 24L * 3600L;    /// Real seconds (not sidereal)
		protected long _totalSeconds;

		public DayTime()
		{
			_totalSeconds = 0;
		}

		public static DayTime ParseFromMeade(String s)
		{
			DayTime result = new DayTime();
			int i = 0;
			long sgn = 1;
			// LOGV2(DEBUG_MEADE, F("DayTime: Parse Coord from [%s]"), s.c_str());
			// Check whether we have a Math.Sign. This should be able to parse RA and DEC strings (RA never has a Math.Sign, and DEC should always have one).
			if ((s[i] == '-') || (s[i] == '+'))
			{
				sgn = s[i] == '-' ? -1 : +1;
				i++;
			}

			// Degs can be 2 or 3 digits
			long degs = s[i++] - '0';
			// LOGV3(DEBUG_MEADE, F("DayTime: 1st digit [%c] -> degs=%l"), s[i - 1], degs);
			degs = degs * 10 + s[i++] - '0';
			// LOGV3(DEBUG_MEADE, F("DayTime: 2nd digit [%c] -> degs=%l"), s[i - 1], degs);

			// Third digit?
			if ((s[i] >= '0') && (s[i] <= '9'))
			{
				degs = degs * 10 + s[i++] - '0';
				// LOGV3(DEBUG_MEADE, F("DayTime: 3rd digit [%c] -> degs=%d"), s[i - 1], degs);
			}
			i++; // Skip seperator

			int mins = int.Parse(s.Substring(i, i + 2));
			// LOGV3(DEBUG_MEADE, F("DayTime: Minutes are [%s] -> mins=%d"), s.Substring(i, i + 2).c_str(), mins);
			int secs = 0;
			if (s.Length > i + 4)
			{
				secs = int.Parse(s.Substring(i + 3, i + 5));
				// LOGV3(DEBUG_MEADE, F("DayTime: Seconds are [%s] -> secs=%d"), s.Substring(i + 3, i + 5).c_str(), secs);
			}
			else
			{
				// LOGV3(DEBUG_MEADE, F("DayTime: No Seconds. slen %d is not > %d"), s.length(), i + 4);
			}
			// Get the Math.Signed total seconds specified....
			result._totalSeconds = sgn * (((degs * 60L + mins) * 60L) + secs);

			// LOGV5(DEBUG_MEADE, F("DayTime: TotalSeconds are %l from %lh %dm %ds"), result._totalSeconds, degs, mins, secs);

			return result;
		}

		public DayTime(DayTime other)
		{
			_totalSeconds = other._totalSeconds;
		}

		public DayTime(int h, int m, int s)
		{

			long sgn = (h < 0) || (m < 0) || (s < 0) ? -1 : 1;
			h = Math.Abs(h);
			m = Math.Abs(m);
			s = Math.Abs(s);
			_totalSeconds = sgn * ((60L * h + m) * 60L + s);
			// Do not call CheckHours() here! Virtual table is not setup yet
		}

		public DayTime(float timeInHours)
		{
			long sgn = Math.Sign(timeInHours);
			timeInHours = Math.Abs(timeInHours);
			_totalSeconds = sgn * (long)Math.Round(timeInHours * 60.0f * 60.0f);
		}

		public int Hours
		{
			get {
				int h, m, s;
				GetTime(out h, out m, out s);
				return h;
			}
		}

		public int Minutes
		{
			get {
				int h, m, s;
				GetTime(out h, out m, out s);
				return m;
			}
		}

		public int Seconds
		{
			get {
				int h, m, s;
				GetTime(out h, out m, out s);
				return s;
			}
		}

		public float TotalHours
		{
			get {
				return 1.0f * _totalSeconds / 3600.0f;
			}
		}

		public float TotalMinutes
		{
			get {
				return 1.0f * _totalSeconds / 60.0f;
			}
		}

		public long TotalSeconds
		{
			get {
				return _totalSeconds;
			}
		}

		public void GetTime(out int h, out int m, out int s)
		{
			long seconds = Math.Abs(_totalSeconds);

			h = (int)(seconds / 3600L);
			seconds = seconds - (h * 3600L);
			m = (int)(seconds / 60L);
			s = (int)(seconds - (m * 60L));

			h *= Math.Sign(_totalSeconds);
		}

		public void SetTime(int h, int m, int s)
		{
			DayTime dt = new DayTime(h, m, s);
			_totalSeconds = dt._totalSeconds;
			CheckHours();
		}

		public void SetTime(DayTime other)
		{
			_totalSeconds = other._totalSeconds;
			CheckHours();
		}

		// Add hours, wrapping days (which are not tracked)
		public void AddHours(int deltaHours)
		{
			_totalSeconds += (long)deltaHours * 3600L;
			CheckHours();
		}

		public virtual void CheckHours()
		{
			while (_totalSeconds >= secondsPerDay)
			{
				_totalSeconds -= secondsPerDay;
			}

			while (_totalSeconds < 0)
			{
				_totalSeconds += secondsPerDay;
			}
		}

		// Add minutes, wrapping hours if needed
		public void AddMinutes(int deltaMins)
		{
			_totalSeconds += deltaMins * 60;
			CheckHours();
		}

		// Add seconds, wrapping minutes and hours if needed
		public void AddSeconds(long deltaSecs)
		{
			_totalSeconds += deltaSecs;
			CheckHours();
		}

		// Add another time, wrapping seconds, minutes and hours if needed
		public void AddTime(DayTime other)
		{
			_totalSeconds += other._totalSeconds;
			CheckHours();
		}

		// Subtract another time, wrapping seconds, minutes and hours if needed
		public void SubtractTime(DayTime other)
		{
			_totalSeconds -= other._totalSeconds;
			CheckHours();
		}

		// Change the hour component
		public bool ChangeHour(int newHour)
		{
			long tsecs = _totalSeconds;
			int hours, mins, secs;
			GetTime(out hours, out mins, out secs);
			var newTime = new DayTime(newHour, mins, secs);
			newTime.CheckHours();
			_totalSeconds = newTime._totalSeconds;
			return tsecs != _totalSeconds;
		}

		// Change the minute component only
		public virtual bool ChangeMinute(int newMins)
		{
			long tsecs = _totalSeconds;
			int hours, mins, secs;
			GetTime(out hours, out mins, out secs);
			var newTime = new DayTime(hours, newMins, secs);
			_totalSeconds = newTime._totalSeconds;
			return tsecs != _totalSeconds;
		}

		// Change the seconds component only
		public virtual bool ChangeSecond(int newSecs)
		{
			long tsecs = _totalSeconds;
			int hours, mins, secs;
			GetTime(out hours, out mins, out secs);
			var newTime = new DayTime(hours, mins, newSecs);
			_totalSeconds = newTime._totalSeconds;
			return tsecs != _totalSeconds;
		}

		// Convert to a standard string (like 14:45:06)
		public override String ToString()
		{
			int hours, mins, secs;
			GetTime(out hours, out mins, out secs);

			int absHours = Math.Abs(hours);
			string sign = _totalSeconds < 0 ? "-" : "";
			string p = $"{sign}{absHours:00}:{mins:00}:{secs:00} ({TotalHours:0.0000})";

			return p;
		}
	}
}
