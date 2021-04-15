using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace OATControl.Converters
{
    /// <summary>
    /// Data binding helper.
    /// Converts a binding to boolean to a Visibility value.
    /// Returns Visibility.Hidden by default when bound value is false.
    /// If Collapse property is true, returns Visibility.Collapsed when bound value is false.
    /// Useful for binding Visibility property to a RadioButton.IsChecked property.
    /// </summary>
    public class OrientationToVisibilityConverter : MarkupExtension, IValueConverter
    {
        /// <summary>
        /// Gets or sets a value indicating whether a false source value is converted to Collapsed or Hidden.
        /// </summary>
        public bool Collapse { get; set; }
        public Orientation VisibleOrientation  { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Visibility v = Visibility.Visible;

            if (this.VisibleOrientation != (Orientation)value)
            {
                v = this.Collapse ? Visibility.Collapsed : Visibility.Hidden;
            }

            return v;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}