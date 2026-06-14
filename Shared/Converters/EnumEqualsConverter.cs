using System.Globalization;
using System.Windows.Data;
using Binding = System.Windows.Data.Binding;

namespace FileCraft.Shared.Converters
{
    public class EnumEqualsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null && parameter != null && value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is true && parameter != null ? parameter : Binding.DoNothing;
        }
    }
}
