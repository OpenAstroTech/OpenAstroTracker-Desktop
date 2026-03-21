using OATCommunications.Utilities;
using System;
using System.ComponentModel;
using System.Xml.Linq;

namespace OATControl.ViewModels
{
	public class PointOfInterest : INotifyPropertyChanged
	{
		private string _name = string.Empty;
		private string _catalogName = string.Empty;
		private float _ra;
		private float _dec;
		private bool _enabled = true;
		private long _raPosition;
		private long _decPosition;
		private bool _isPositionCalculated;
		private double _distance;
		private bool _isOutOfDECRange;

		public event PropertyChangedEventHandler? PropertyChanged;

		public PointOfInterest()
		{
		}

		public PointOfInterest(string name)
		{
			_name = name;
		}

		public PointOfInterest(XElement element)
		{
			_name = element.Attribute("Name")?.Value ?? string.Empty;
			_catalogName = element.Attribute("CatalogName")?.Value ?? string.Empty;

			var raStr = element.Attribute("RA")?.Value;
			if (raStr != null)
			{
				var raParts = raStr.Split(':');
				if (raParts.Length == 3)
				{
					int h = int.Parse(raParts[0]);
					int m = int.Parse(raParts[1]);
					int s = int.Parse(raParts[2]);
					_ra = h + m / 60.0f + s / 3600.0f;
				}
			}

			var decStr = element.Attribute("DEC")?.Value;
			if (decStr != null)
			{
				var decParts = decStr.Split(':');
				if (decParts.Length == 3)
				{
					int d = int.Parse(decParts[0]);
					int m = int.Parse(decParts[1]);
					int s = int.Parse(decParts[2]);
					float sign = d < 0 ? -1.0f : 1.0f;
					_dec = sign * (Math.Abs(d) + m / 60.0f + s / 3600.0f);
				}
			}

			var enabledStr = element.Attribute("Enabled")?.Value;
			_enabled = enabledStr == null || enabledStr != "false";
		}

		public string Name
		{
			get => _name;
			set { _name = value; OnPropertyChanged(nameof(Name)); }
		}

		public string CatalogName
		{
			get => _catalogName;
			set { _catalogName = value; OnPropertyChanged(nameof(CatalogName)); }
		}

		public float RA
		{
			get => _ra;
			set { _ra = value; OnPropertyChanged(nameof(RA)); }
		}

		public float DEC
		{
			get => _dec;
			set { _dec = value; OnPropertyChanged(nameof(DEC)); }
		}

		public bool Enabled
		{
			get => _enabled;
			set { _enabled = value; OnPropertyChanged(nameof(Enabled)); }
		}

		public long RAPosition => _raPosition;
		public long DECPosition => _decPosition;
		public bool IsPositionCalculated => _isPositionCalculated;

		public double Distance
		{
			get => _distance;
			private set { _distance = value; OnPropertyChanged(nameof(Distance)); }
		}

		public bool IsOutOfDECRange
		{
			get => _isOutOfDECRange;
			set { _isOutOfDECRange = value; OnPropertyChanged(nameof(IsOutOfDECRange)); }
		}

		public void SetPositions(long ra, long dec)
		{
			_raPosition = ra;
			_decPosition = dec;
			_isPositionCalculated = true;
		}

		public void CalcDistancesFrom(double currentRA, double currentDEC, float raStepper, float decStepper)
		{
			if (_isPositionCalculated)
			{
				double raDist = _raPosition - raStepper;
				double decDist = _decPosition - decStepper;
				Distance = Math.Sqrt(raDist * raDist + decDist * decDist);
			}
			else
			{
				double raDist = _ra - currentRA;
				double decDist = _dec - currentDEC;
				Distance = Math.Sqrt(raDist * raDist + decDist * decDist);
			}
		}

		public XElement ToXElement()
		{
			int rH = (int)_ra;
			int rM = (int)((Math.Abs(_ra) - Math.Abs(rH)) * 60);
			int rS = (int)((Math.Abs(_ra) - Math.Abs(rH) - rM / 60.0f) * 3600);

			int dD = (int)_dec;
			int dM = (int)((Math.Abs(_dec) - Math.Abs(dD)) * 60);
			int dS = (int)((Math.Abs(_dec) - Math.Abs(dD) - dM / 60.0f) * 3600);

			return new XElement("Object",
				new XAttribute("CatalogName", _catalogName),
				new XAttribute("Name", _name),
				new XAttribute("RA", $"{rH:00}:{rM:00}:{rS:00}"),
				new XAttribute("DEC", $"{dD:00}:{dM:00}:{dS:00}"),
				_enabled ? null : new XAttribute("Enabled", "false"));
		}

		protected void OnPropertyChanged(string name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}