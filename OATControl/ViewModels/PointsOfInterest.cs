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

		public void CalcDistancesFrom(double ra, double dec)
		{
			foreach (var point in this)
			{
				point.CalcDistancesFrom(ra, dec);
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
	}
}
