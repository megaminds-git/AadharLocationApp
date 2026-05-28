using System.Globalization;
using System.Windows.Data;

namespace AadharLocation.AdminDashboard.Converters;

[ValueConversion(typeof(DateTime), typeof(string))]
public class DateTimeFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string fmt = parameter as string ?? "dd MMM yyyy HH:mm";
        return value switch
        {
            DateTime dt => dt.ToLocalTime().ToString(fmt, culture),
            DateTimeOffset dto => dto.LocalDateTime.ToString(fmt, culture),
            _ => string.Empty
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

[ValueConversion(typeof(DateTime?), typeof(string))]
public class NullableDateTimeFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string fmt = parameter as string ?? "dd MMM yyyy HH:mm";
        string fallback = "Never";
        return value switch
        {
            DateTime dt => dt.ToLocalTime().ToString(fmt, culture),
            DateTimeOffset dto => dto.LocalDateTime.ToString(fmt, culture),
            null => fallback,
            _ => fallback
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class BoolToAcknowledgedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool b && b ? "Acknowledged" : "Pending";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
