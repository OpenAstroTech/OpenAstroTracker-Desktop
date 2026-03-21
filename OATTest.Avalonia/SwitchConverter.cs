using Avalonia.Data.Converters;
using Avalonia.Metadata;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace OATTest.Converters
{
    public class SwitchConverter : IValueConverter
    {
        [Content]
        public List<SwitchConverterCase> Cases { get; set; } = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null) return null;
            foreach (var c in Cases)
                if (value.ToString()!.ToUpper() == c.When?.ToUpper())
                    return c.Then;
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class SwitchConverterCase
    {
        public string? When { get; set; }

        [Content]
        public object? Then { get; set; }
    }
}
