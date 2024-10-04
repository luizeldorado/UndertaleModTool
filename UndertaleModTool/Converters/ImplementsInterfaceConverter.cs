using System;
using System.Globalization;
using System.Windows.Data;

namespace UndertaleModTool
{
    [ValueConversion(typeof(object), typeof(bool))]
    public class ImplementsInterfaceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Type iface = (Type)parameter;
            return iface.IsAssignableFrom(value?.GetType());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
