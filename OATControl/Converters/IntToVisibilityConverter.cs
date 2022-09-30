using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace OATControl.Converters
{
    /// <summary>
    /// Data binding helper.
    /// Converts a binding from boolean to an integer value.
    /// Returns FalseValue when bound value is false and TrueValue when value is ture.
    /// </summary>
    public class IntToVisibilityConverter : MarkupExtension, IValueConverter
    {
        /// <summary>
        /// Gets or sets the integer value to return on the given boolean value.
        /// </summary>
        public int VisibleValue { get; set; }
        public bool UseCollapse { get; set; }
        public string Operator { get; set; }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            long val = value is long ? (long)value : (long)(int)value;
            Visibility visualState = UseCollapse ? Visibility.Collapsed : Visibility.Hidden;
			long visibleVal = this.VisibleValue;
			if (parameter != null && parameter is string)
			{
				long.TryParse((string)parameter, out visibleVal);
			}
			if (parameter != null && parameter is long)
			{
				visibleVal = (long)parameter;
			}
			if (parameter != null && parameter is int)
			{
				visibleVal = (long)(int)parameter;
			}
			if (Operator.Equals(">"))
            {
                if (val > visibleVal) visualState = Visibility.Visible;
            }
            if (Operator.Equals(">="))
            {
                if (val >= visibleVal) visualState = Visibility.Visible;
            }
            if (Operator.Equals("<"))
            {
                if (val < visibleVal) visualState = Visibility.Visible;
            }
            if (Operator.Equals("<="))
            {
                if (val <= visibleVal) visualState = Visibility.Visible;
            }
            if (Operator.Equals("=="))
            {
                if (val == visibleVal) visualState = Visibility.Visible;
            }
            if (Operator.Equals("!="))
            {
                if (val != visibleVal) visualState = Visibility.Visible;
            }

            return visualState;
        }

        /// <summary>
        /// Converts a value back. Not implemented
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (Visibility)value== Visibility.Visible ? this.VisibleValue : 0;
        }

        /// <summary>
        /// When implemented in a derived class, returns an object that is provided as the value of the target property for this markup extension.
        /// </summary>
        /// <param name="serviceProvider">A service provider helper that can provide services for the markup extension.</param>
        /// <returns>
        /// The object value to set on the property where the extension is applied.
        /// </returns>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}