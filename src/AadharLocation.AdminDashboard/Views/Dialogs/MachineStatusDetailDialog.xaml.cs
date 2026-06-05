using System.Windows;
using AadharLocation.Shared.DTOs.Machines;
using AadharLocation.Shared.Enums;

namespace AadharLocation.AdminDashboard.Views.Dialogs;

public partial class MachineStatusDetailDialog : Window
{
    public List<MachineDetailItem> Items { get; }

    public MachineStatusDetailDialog(string title, IEnumerable<MachineDto> machines)
    {
        Items = machines.Select(m => new MachineDetailItem(m)).ToList();
        InitializeComponent();
        Title = title;
        DataContext = this;
    }
}

public sealed class MachineDetailItem
{
    public MachineStatus Status { get; }
    public string Name { get; }
    public string OperatorName { get; }
    public string Coordinates { get; }
    public string LastSeen { get; }

    public MachineDetailItem(MachineDto m)
    {
        Status = m.Status;
        Name = m.Name;
        OperatorName = m.AssignedOperatorName ?? "Unassigned";
        Coordinates = m.CurrentLatitude.HasValue
            ? $"Lat: {m.CurrentLatitude:F4}°  |  Lon: {m.CurrentLongitude:F4}°"
            : "No GPS data";
        LastSeen = m.LastSeenAt.HasValue
            ? m.LastSeenAt.Value.ToString("dd MMM yyyy HH:mm")
            : "Never";
    }
}
