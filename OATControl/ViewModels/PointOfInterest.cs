using OATCommunications.Utilities;
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
		private long targetRAPos;
		private long targetDECPos;
		private bool positionsCalculated;
		private double raDistance;
		private double decDistance;
		private double decStepDistance;
		private double raStepDistance;
		private double distance;
		private double distanceNormalized;
		private bool _reachable;

		public event PropertyChangedEventHandler PropertyChanged;

		public PointOfInterest(string name)
		{
			Name = name;
			RA = 0;
			DEC = 90;
			positionsCalculated = false;
		}

		public PointOfInterest(XElement e)
		{
			positionsCalculated = false;
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

		internal void CalcDistancesFrom(double ra, double dec, long raPos, long decPos)
		{
			RADistance = Math.Abs((RA % 12) - (ra % 12));
			if (RADistance > 6)
			{
				RADistance = 12 - RADistance;
			}
			DECDistance = Math.Abs(DEC - dec);
			if (IsPositionCalculated)
			{
				double raLen = Math.Abs(raPos - targetRAPos);
				double decLen = Math.Abs(decPos - targetDECPos);
				distance = Math.Sqrt(raLen * raLen + decLen * decLen);
				raStepDistance = (long)Math.Abs(raPos - targetRAPos);
				decStepDistance = (long)Math.Abs(decPos - targetDECPos);
			}
			else
			{
				var DECDistanceCalc = Math.Abs(((DEC - dec) / 180) * 6);
				distance = 10 * Math.Sqrt(RADistance * RADistance + DECDistanceCalc * DECDistanceCalc);
				raStepDistance = (long)Math.Abs(RADistance);//stepps per deged
				decStepDistance = (long)Math.Abs(decPos - targetDECPos);
			}
			// Log.WriteLine("POINT: {0} is at {1:0.00}, {2:0.00} (Stepper: {3}, {4}). From {5:0.00}, {6:0.00} ({7}, {8}) it is {9:0.000} away.", this.Name, this.RA, this.DEC, this.RAPosition, this.DECPosition, ra, dec, raPos, decPos, this.Distance);
		}

		void OnPropertyChanged([CallerMemberName] string prop = "")
		{
			var handler = this.PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(prop));
			}
		}

		public void Normalize(double maxRaPos, double maxDecPos, double maxDist)
		{
			if (positionsCalculated)
			{
				decStepDistance = 100.0 * DECStepDistance / maxDecPos;
				raStepDistance = 100.0 * RAStepDistance / maxRaPos;
				distanceNormalized = 100.0 * Distance / maxDist;
			}
			else
			{
				distanceNormalized = 0;
			}
			OnPropertyChanged("DECStepDistance");
			OnPropertyChanged("RAStepDistance");
			OnPropertyChanged("DistanceNormalized");
		}

		public bool IsPositionCalculated { get { return positionsCalculated; } }
		public void SetPositions(long ra, long dec)
		{
			targetRAPos = ra;
			targetDECPos = dec;
			positionsCalculated = true;
		}

		public long RAPosition => targetRAPos;
		public long DECPosition => targetDECPos;
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

		public double DECStepDistance
		{
			get { return decStepDistance; }
			set
			{
				if (decStepDistance != value)
				{
					decStepDistance = value;
					OnPropertyChanged();
				}
			}
		}

		public double RAStepDistance
		{
			get { return raStepDistance; }
			set
			{
				if (raStepDistance != value)
				{
					raStepDistance = value;
					OnPropertyChanged();
				}
			}
		}

		

		public double DistanceNormalized
		{
			get { return distanceNormalized; }
			set
			{
				if (distanceNormalized != value)
				{
					distanceNormalized = value;
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

		public bool IsReachable
		{
			get { return _reachable; }
			set
			{
				if (_reachable != value)
				{
					_reachable = value;
					OnPropertyChanged();
				}
			}
		}

	}
}