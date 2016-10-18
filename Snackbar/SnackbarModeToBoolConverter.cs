using System;
using System.Globalization;
using System.Windows.Data;

namespace Snackbar
{
    public class SnackbarModeToBoolConverter : IValueConverter
    {
        public SnackbarMode TrueValue { get; set; } = SnackbarMode.Automatic;
        public SnackbarMode FalseValue { get; set; } = SnackbarMode.Manual;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (SnackbarMode)value == TrueValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? TrueValue : FalseValue;
        }
    }
}
