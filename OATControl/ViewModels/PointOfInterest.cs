using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OATControl.ViewModels
{
	public class PointOfInterest : INotifyPropertyChanged
	{
		private double raDistance;
		private double decDistance;
		private double distance;

		public event PropertyChangedEventHandler PropertyChanged;

		public PointOfInterest(XElement e)
		{
			float h, m, s;
			Name = e.Attribute("Name").Value;
			var ra = e.Attribute("RA").Value.Split(":".ToCharArray());
			if ((ra.Length == 3) && float.TryParse(ra[0], out h) && float.TryParse(ra[1], out m) && float.TryParse(ra[2], out s))
			{
				RA = h + m / 60.0f + s / 3600.0f;
			}
			var dec = e.Attribute("DEC").Value.Split(":".ToCharArray());
			if ((dec.Length == 3) && float.TryParse(dec[0], out h) && float.TryParse(dec[1], out m) && float.TryParse(dec[2], out s))
			{
				DEC = h + m / 60.0f + s / 3600.0f;
			}
		}

		internal void CalcDistancesFrom(double ra, double dec)
		{
			RADistance = Math.Abs((RA % 12) - (ra % 12));
			if (RADistance > 6)
			{
				RADistance = 12 - RADistance;
			}
			DECDistance = Math.Abs(DEC - dec);
			var DECDistanceCalc = Math.Abs(((DEC - dec) / 180) * 6);
			Distance = Math.Sqrt(RADistance * RADistance + DECDistanceCalc * DECDistanceCalc);
		}

		public PointOfInterest(string name)
		{
			Name = name;
			RA = 0;
			DEC = 90;
		}

		void OnPropertyChanged([CallerMemberName] string prop = "")
		{
			var handler = this.PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(prop));
			}
		}

		public string Name { get; }
		public double RA { get; }
		public double RADistance
		{
			get { return raDistance; }
			set
			{
				if (raDistance != value)
				{
					raDistance = value;
					OnPropertyChanged();
				}
			}
		}
		public double DEC { get; }
		public double DECDistance
		{
			get { return decDistance; }
			set
			{
				if (decDistance != value)
				{
					decDistance = value;
					OnPropertyChanged();
				}
			}
		}

		public double Distance
		{
			get { return distance; }
			set
			{
				if (distance != value)
				{
					distance = value;
					OnPropertyChanged();
				}
			}
		}

	}
}
