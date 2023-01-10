using OATCommunications.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OATCommunications.Model
{
    public static class Parsers
    {

		public static bool TryParseRA(string ra, out double dRa)
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

		public static bool TryParseDec(string dec, out double dDec)
		{
			try
			{
				var parts = dec.Split('*', '\'');
				var sign = 1.0;
				if (parts[0][0] == '-')
				{
					dDec = int.Parse(parts[0].Substring(1)) + int.Parse(parts[1]) / 60.0;
					sign = -1.0;
				}
				else
				{
					dDec = int.Parse(parts[0]) + int.Parse(parts[1]) / 60.0;
				}
				if (parts.Length > 2)
				{
					dDec += int.Parse(parts[2]) / 3600.0;
				}

				dDec = sign * dDec;
				return true;
			}
			catch (Exception ex)
			{
				Log.WriteLine("OAT: Can't parse DEC from {0}", dec);
			}
			dDec = 0;
			return false;
		}

	}
}
