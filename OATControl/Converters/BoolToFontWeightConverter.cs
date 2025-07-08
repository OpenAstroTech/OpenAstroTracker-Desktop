using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace OATControl.Converters
{
    public class BoolToFontWeightConverter : MarkupExtension, IValueConverter
    {
        public FontWeight TrueWeight { get; set; } = FontWeights.Bold;
        public FontWeight FalseWeight { get; set; } = FontWeights.Normal;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? TrueWeight : FalseWeight;
            return FalseWeight;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
} 