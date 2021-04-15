using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Threading.Tasks;

namespace OATControl.ViewModels
{
	public class PointsOfInterest : List<PointOfInterest>
	{
		private long _decLowLimit = -99999999;
		private long _decHighLimit = 99999999;

		public PointsOfInterest()
		{
		}

		public PointsOfInterest(IEnumerable<PointOfInterest> i)
		{
			foreach (var pt in i)
			{
				this.Add(pt);
			}
		}

		public void ReadFromXml(string poiFile)
		{
			XDocument doc = XDocument.Load(poiFile);
			var pointsOfInterest = doc.Element("PointsOfInterest").Elements("Object").Select(e => new PointOfInterest(e)).ToList();

			pointsOfInterest.Sort((p1, p2) =>
			{
				return p1.Name.CompareTo(p2.Name);
			});

			foreach (var point in pointsOfInterest)
			{
				this.Add(point);
			}
		}

		public void CalcDistancesFrom(double ra, double dec, long raPos, long decPos)
		{
			foreach (var point in this)
			{
				point.CalcDistancesFrom(ra, dec, raPos, decPos);
			}

			double maxDecPos = this.Max(p => Math.Abs(p.DECPosition - decPos));
			double maxRaPos = this.Max(p => Math.Abs(p.RAPosition - raPos));
			double maxDist = this.Max(p => p.Distance);
			foreach (var point in this)
			{
				point.Normalize(maxRaPos, maxDecPos, maxDist);
			}
		}

		public void SortBy(string field)
		{
			if (field == "Name")
			{
				this.Sort((p1, p2) => { return p1.Name.CompareTo(p2.Name); });
			}
			if (field == "Distance")
			{
				this.Sort((p1, p2) => { return p1.Distance.CompareTo(p2.Distance); });
			}
		}

		internal void SetDecLowStepLimit(long decStepper)
		{
			_decLowLimit = decStepper;
			CheckPointReachability();
		}

		internal void SetDecHighStepLimit(long decStepper)
		{
			_decHighLimit = decStepper;
			CheckPointReachability();
		}

		void CheckPointReachability()
		{
			foreach (var point in this)
			{
				point.IsReachable = (point.DECPosition >= _decLowLimit) && (point.DECPosition <= _decHighLimit);
			}
		}
	}
}
