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
            double len = 1;
            if (value is double)
            {
                if (Inverter != 0)
				{
                    len=(Inverter-(double)value) / Divisor;
                }
				else
				{
                    len = (double)value / Divisor;
                }
            }
            return new GridLength(len, GridUnitType.Star);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
