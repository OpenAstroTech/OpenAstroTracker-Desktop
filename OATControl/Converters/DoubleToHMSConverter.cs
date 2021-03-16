using System;
using System.Globalization;
using System.Windows.Data;
using System.Xml.Schema;

namespace OATControl.Converters
{
	public class DoubleToHMSConverter : IValueConverter
	{
		public DoubleToHMSConverter()
		{
			Formatter = "{0:00}:{1:00}:{2:00}";
		}

		public string Formatter { get; set; }

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is double)
			{
				double total = Math.Abs((double)value);
				int h = (int)total;
				total = (total - h) * 60;
				int m = (int)total;
				total = (total - m) * 60;
				int s = (int)total;
				h = h * Math.Sign((double)value);
				string formatter = Formatter.Replace('[', '{').Replace(']', '}');
				return string.Format(formatter, h, m, s);
			}
			return "";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
