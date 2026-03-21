using System;
using OATControl;

namespace OATControl.ViewModels
{
    public static class CoordHelper
    {
        public enum CoordSeparators { NoSeparators, RaSeparators, DecSeparators, Colons }

        public static string CoordToString(double dpos, CoordSeparators sep = CoordSeparators.NoSeparators)
        {
            float pos = (float)dpos;
            switch (sep)
            {
                case CoordSeparators.Colons:
                {
                    var ra = new DayTime(pos);
                    ra.GetTime(out int h, out int m, out int s);
                    return $"{h:00}:{m:00}:{s:00}";
                }
                case CoordSeparators.RaSeparators:
                {
                    var ra = new DayTime(pos);
                    ra.GetTime(out int h, out int m, out int s);
                    string sign = ra.TotalSeconds < 0 ? "-" : "";
                    return $"{sign}{Math.Abs(h):00}h {m:00}m {s:00}s";
                }
                case CoordSeparators.DecSeparators:
                {
                    var dec = new Declination(pos);
                    dec.GetTime(out int d, out int m, out int s);
                    string sign = dec.TotalSeconds < 0 ? "-" : "";
                    return $"{sign}{Math.Abs(d):00}° {m:00}\" {s:00}'";
                }
                default:
                {
                    var ra = new DayTime(pos);
                    ra.GetTime(out int h, out int m, out int s);
                    return $"{h} {m} {s}";
                }
            }
        }

        public static bool TryParseCoord(string pos, out float result)
        {
            result = 0;
            if (string.IsNullOrEmpty(pos)) return false;
            var parts = pos.Split("hms: \"'°".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3) return false;
            if (!int.TryParse(parts[0], out int hours)) return false;
            float sign = 1.0f;
            if (hours < 0) { hours = Math.Abs(hours); sign = -1.0f; }
            if (!int.TryParse(parts[1], out int minutes)) return false;
            if (!int.TryParse(parts[2], out int seconds)) return false;
            result = sign * (hours + minutes / 60.0f + seconds / 3600.0f);
            return true;
        }
    }
}
