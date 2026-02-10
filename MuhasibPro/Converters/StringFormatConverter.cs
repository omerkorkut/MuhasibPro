using Microsoft.UI.Xaml.Data;
using System;
using Windows.UI.Xaml.Data;

namespace MuhasibPro.Converters
{
    public class StringFormatConverter : IValueConverter
    {
        public string Format { get; set; } = "{0}";

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
                return string.Empty;

            return string.Format(Format, value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}