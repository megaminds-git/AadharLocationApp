using System.Globalization;
using System.Windows.Data;

namespace AadharLocation.AdminDashboard.Converters;

public class StringNotEqualConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        values.Length == 2 && values[0] is string a && values[1] is string b &&
        !string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
