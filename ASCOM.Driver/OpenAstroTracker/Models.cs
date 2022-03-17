using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static ASCOM.OpenAstroTracker.SharedResources;

namespace ASCOM.OpenAstroTracker
{
    public class ProfileData {
        public bool TraceState;
        public LoggingFlags TraceFlags;
        public string ComPort;
        public long BaudRate;
        public double Latitude;
        public double Longitude;
        public double Elevation; 
    }
}
