using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using AadharLocation.Shared.Enums;

namespace AadharLocation.AdminDashboard.Converters;

public class MachineStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MachineStatus status)
        {
            return status switch
            {
                MachineStatus.Online  => new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80)),
                MachineStatus.Offline => new SolidColorBrush(Color.FromRgb(0xF8, 0x71, 0x71)),
                MachineStatus.Idle    => new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24)),
                _ => Brushes.Gray
            };
        }
        if (value is OperatorStatus opStatus)
        {
            return opStatus switch
            {
                OperatorStatus.Active   => new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80)),
                OperatorStatus.Inactive => new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF)),
                _ => Brushes.Gray
            };
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class AlertTypeToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is AlertType alertType)
        {
            return alertType switch
            {
                AlertType.GeofenceBreach => new SolidColorBrush(Color.FromRgb(0xF8, 0x71, 0x71)),
                AlertType.Offline        => new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24)),
                _ => Brushes.Gray
            };
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
