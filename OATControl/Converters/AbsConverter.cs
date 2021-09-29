using System;
using System.Globalization;
using System.Windows.Data;
using System.Xml.Schema;

namespace OATControl.Converters
{
	public class AbsConverter : IValueConverter
	{
		public AbsConverter()
		{
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is double)
			{
				return Math.Abs((double)value);
			}
			if (value is float)
			{
				return Math.Abs((float)value);
			}
			if (value is int)
			{
				return (int)Math.Abs((int)value);
			}
			if (value is long)
			{
				return (long)Math.Abs((long)value);
			}
			return 0;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (targetType.Name == "Int32")
			{
				return int.Parse((string)value);
			}
			throw new NotImplementedException();
		}
	}
}
