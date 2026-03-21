using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace OATControl.ViewModels
{
	public class PointsOfInterest : List<PointOfInterest>
	{
		private long _decLowStepLimit = long.MinValue;
		private long _decHighStepLimit = long.MaxValue;

		public PointsOfInterest()
		{
		}

		public PointsOfInterest(IEnumerable<PointOfInterest> items) : base(items)
		{
		}

		public void ReadFromXml(string filePath)
		{
			var doc = XDocument.Load(filePath);
			var root = doc.Element("PointsOfInterest");
			if (root == null) return;
			foreach (var elem in root.Elements("Object"))
			{
				Add(new PointOfInterest(elem));
			}
		}

		public void WriteToXml(string filePath)
		{
			var doc = new XDocument(
				new XElement("PointsOfInterest",
					this.Select(p => p.ToXElement())));
			doc.Save(filePath);
		}

		public void CalcDistancesFrom(double currentRA, double currentDEC, float raStepper, float decStepper)
		{
			foreach (var point in this)
			{
				point.CalcDistancesFrom(currentRA, currentDEC, raStepper, decStepper);
				if (point.IsPositionCalculated)
				{
					point.IsOutOfDECRange = point.DECPosition < _decLowStepLimit || point.DECPosition > _decHighStepLimit;
				}
			}
		}

		public void SortBy(string property)
		{
			if (property == "Distance")
			{
				Sort((a, b) => a.Distance.CompareTo(b.Distance));
			}
		}

		public void SetDecLowStepLimit(long limit)
		{
			_decLowStepLimit = limit;
		}

		public void SetDecHighStepLimit(long limit)
		{
			_decHighStepLimit = limit;
		}
	}
}
