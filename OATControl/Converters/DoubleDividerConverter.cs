using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OATControl.Converters
{
    public class DoubleDividerConverter : IValueConverter
    {
        public DoubleDividerConverter()
        {
            Divisor = 2.0;
            Addition = 0.0;
        }

        public double Divisor { get; set; }

        public double Addition { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double)
            {
                return ((double)value) / Divisor;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DoubleToGridLengthDividerConverter : IValueConverter
    {
        public DoubleToGridLengthDividerConverter()
        {
            Divisor = 1.0;
            Inverter = 0;
        }

        public double Divisor { get; set; }
        public double Inverter { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double)
            {
                if (Inverter != 0)
				{
                    return new GridLength((Inverter-(double)value) / Divisor, GridUnitType.Star);
                }
                return new GridLength((double)value / Divisor, GridUnitType.Star);
            }
            return new GridLength(10);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
