using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AadharLocation.AdminDashboard.Converters;

public class BoolToStringConverter : IValueConverter
{
    public string TrueValue  { get; set; } = "True";
    public string FalseValue { get; set; } = "False";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool b && b ? TrueValue : FalseValue;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value?.ToString() == TrueValue;
}

public class BoolToBrushConverter : IValueConverter
{
    public SolidColorBrush? TrueBrush  { get; set; }
    public SolidColorBrush? FalseBrush { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (value is bool b && b ? TrueBrush : FalseBrush) ?? Brushes.Transparent;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isNull = value == null;
        if (parameter is string s && s == "Inverse") isNull = !isNull;
        return isNull
            ? System.Windows.Visibility.Collapsed
            : System.Windows.Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class StringNotEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool hasValue = !string.IsNullOrEmpty(value?.ToString());
        if (parameter is string s && s == "Inverse") hasValue = !hasValue;
        return hasValue
            ? System.Windows.Visibility.Visible
            : System.Windows.Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
