using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OATControl.ViewModels
{
	class AstroTools
	{
		const double DEG_TO_RAD = Math.PI / 180.0;
		const double RAD_TO_DEG = 180.0 / Math.PI;

		public static double JulianDate(DateTime utc)
		{
			int year = utc.Year;
			int month = utc.Month;
			double day = utc.Day + utc.TimeOfDay.TotalHours / 24.0;

			if (month <= 2)
			{
				year--;
				month += 12;
			}

			int A = year / 100;
			int B = 2 - A + (A / 4);

			double JD = Math.Floor(365.25 * (year + 4716)) +
						Math.Floor(30.6001 * (month + 1)) +
						day + B - 1524.5;

			return JD;
		}

		public static double GreenwichSiderealTime(double jd)
		{
			double T = (jd - 2451545.0) / 36525.0;

			double gst = 280.46061837 +
						 360.98564736629 * (jd - 2451545.0) +
						 0.000387933 * T * T -
						 (T * T * T) / 38710000.0;

			gst = gst % 360.0;
			if (gst < 0) gst += 360.0;

			return gst; // in degrees
		}

		public static double NormalizeDegrees(double angle)
		{
			while (angle < 0) angle += 360;
			while (angle >= 360) angle -= 360;
			return angle;
		}

		public static void AzAltToRaDec(
			double azDeg, double altDeg,
			double latDeg, double lonDeg,
			DateTime utc,
			out double raDeg, out double decDeg)
		{
			double az = azDeg * DEG_TO_RAD;
			double alt = altDeg * DEG_TO_RAD;
			double lat = latDeg * DEG_TO_RAD;

			double jd = JulianDate(utc);
			double gstDeg = GreenwichSiderealTime(jd);
			double lstDeg = NormalizeDegrees(gstDeg + lonDeg);
			double lstRad = lstDeg * DEG_TO_RAD;

			// Declination
			double sinDec = Math.Sin(alt) * Math.Sin(lat) +
							Math.Cos(alt) * Math.Cos(lat) * Math.Cos(az);
			double dec = Math.Asin(sinDec);

			// Hour angle
			double cosHA = (Math.Sin(alt) - Math.Sin(lat) * sinDec) /
						   (Math.Cos(lat) * Math.Cos(dec));

			// Clamp to [-1, 1] to avoid NaN due to floating point error
			cosHA = Math.Max(-1.0, Math.Min(1.0, cosHA));
			double ha = Math.Acos(cosHA);

			// Determine correct quadrant of HA using azimuth
			if (Math.Sin(az) > 0)
				ha = 2 * Math.PI - ha;

			// Right Ascension
			double ra = lstRad - ha;
			if (ra < 0) ra += 2 * Math.PI;

			raDeg = ra * RAD_TO_DEG;
			decDeg = dec * RAD_TO_DEG;
		}

		public static void RaDecToAzAlt(
			double raDeg, double decDeg,
			double latDeg, double lonDeg,
			DateTime utc,
			out double azDeg, out double altDeg)
		{
			double ra = raDeg * DEG_TO_RAD;
			double dec = decDeg * DEG_TO_RAD;
			double lat = latDeg * DEG_TO_RAD;

			// Sidereal time
			double jd = JulianDate(utc);
			double gstDeg = GreenwichSiderealTime(jd);
			double lstDeg = NormalizeDegrees(gstDeg + lonDeg);
			double lst = lstDeg * DEG_TO_RAD;

			// Hour angle
			double ha = lst - ra;
			ha = NormalizeRadians(ha);

			// Altitude
			double sinAlt = Math.Sin(dec) * Math.Sin(lat) +
							Math.Cos(dec) * Math.Cos(lat) * Math.Cos(ha);
			double alt = Math.Asin(sinAlt);

			// Azimuth
			double cosAz = (Math.Sin(dec) - Math.Sin(alt) * Math.Sin(lat)) /
						   (Math.Cos(alt) * Math.Cos(lat));
			cosAz = Math.Max(-1.0, Math.Min(1.0, cosAz));  // Clamp
			double az = Math.Acos(cosAz);

			// Resolve azimuth quadrant
			if (Math.Sin(ha) > 0)
				az = 2 * Math.PI - az;

			azDeg = az * RAD_TO_DEG;
			altDeg = alt * RAD_TO_DEG;
		}

		private static double NormalizeRadians(double angle)
		{
			while (angle < 0) angle += 2 * Math.PI;
			while (angle >= 2 * Math.PI) angle -= 2 * Math.PI;
			return angle;
		}
	}
}
