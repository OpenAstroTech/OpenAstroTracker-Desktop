using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;

namespace OATTest.Converters
{
    /// <summary>
    /// A converter that accepts <see cref="SwitchConverterCase"/>s and converts them to the 
    /// Then property of the case.
    /// </summary>
    [ContentProperty("Cases")]
    public class SwitchConverter : IValueConverter
    {
        // Converter instances.
        List<SwitchConverterCase> _cases;

        #region Public Properties.
        /// <summary>
        /// Gets or sets an array of <see cref="SwitchConverterCase"/>s that this converter can use to produde values from.
        /// </summary>
        public List<SwitchConverterCase> Cases { get { return _cases; } set { _cases = value; } }
        #endregion
        #region Construction.
        /// <summary>
        /// Initializes a new instance of the <see cref="SwitchConverter"/> class.
        /// </summary>
        public SwitchConverter()
        {
            // Create the cases array.
            _cases = new List<SwitchConverterCase>();
        }
        #endregion

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
            // This will be the results of the operation.
            object results = null;

            // I'm only willing to convert SwitchConverterCases in this converter and no nulls!
            if (value == null) throw new ArgumentNullException("value");

            // I need to find out if the case that matches this value actually exists in this converters cases collection.
            if (_cases != null && _cases.Count > 0)
                for (int i = 0; i < _cases.Count; i++)
                {
                    // Get a reference to this case.
                    SwitchConverterCase targetCase = _cases[i];

                    // Check to see if the value is the cases When parameter.
                    if (value == targetCase || value.ToString().ToUpper() == targetCase.When.ToString().ToUpper())
                    {
                        // We've got what we want, the results can now be set to the Then property
                        // of the case we're on.
                        results = targetCase.Then;

                        // All done, get out of the loop.
                        break;
                    }
                }

            // return the results.
            return results;
        }

        /// <summary>
        /// Converts a value.
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
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Represents a case for a switch converter.
    /// </summary>
    [ContentProperty("Then")]
    public class SwitchConverterCase
    {
        // case instances.
        string _when;
        object _then;

        #region Public Properties.
        /// <summary>
        /// Gets or sets the condition of the case.
        /// </summary>
        public string When { get { return _when; } set { _when = value; } }
        /// <summary>
        /// Gets or sets the results of this case when run through a <see cref="SwitchConverter"/>
        /// </summary>
        public object Then { get { return _then; } set { _then = value; } }
        #endregion
        #region Construction.
        /// <summary>
        /// Switches the converter.
        /// </summary>
        public SwitchConverterCase()
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="SwitchConverterCase"/> class.
        /// </summary>
        /// <param name="when">The condition of the case.</param>
        /// <param name="then">The results of this case when run through a <see cref="SwitchConverter"/>.</param>
        public SwitchConverterCase(string when, object then)
        {
            // Hook up the instances.
            this._then = then;
            this._when = when;
        }
        #endregion

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("When={0}; Then={1}", When.ToString(), Then.ToString());
        }
    }
}
