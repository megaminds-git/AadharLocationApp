using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using AadharLocation.AdminDashboard.Infrastructure;
using AadharLocation.Shared.DTOs.Machines;
using AadharLocation.Shared.Enums;
using Microsoft.Win32;

namespace AadharLocation.AdminDashboard.Views.Dialogs;

public partial class MachineStatusDetailDialog : Window
{
    public List<MachineDetailItem> Items { get; }

    private readonly IGeocodingService? _geocoding;

    public MachineStatusDetailDialog(string title, IEnumerable<MachineDto> machines, IGeocodingService? geocoding = null)
    {
        Items      = machines.Select(m => new MachineDetailItem(m)).ToList();
        _geocoding = geocoding;
        InitializeComponent();
        Title       = title;
        DataContext = this;
        Loaded     += async (_, _) => await LoadCitiesAsync();
    }

    private async Task LoadCitiesAsync()
    {
        if (_geocoding is null) return;

        var tasks = Items
            .Where(i => i.HasCoordinates)
            .Select(async item =>
            {
                try
                {
                    var city = await _geocoding.GetCityAsync(item.Lat, item.Lon);
                    if (!string.IsNullOrEmpty(city))
                        item.City = city;
                }
                catch { /* silently skip */ }
            });

        await Task.WhenAll(tasks);
    }

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            FileName   = $"{Title.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmm}",
            DefaultExt = ".csv",
            Filter     = "CSV files (*.csv)|*.csv"
        };

        if (dialog.ShowDialog(this) != true) return;

        var sb = new StringBuilder();
        sb.AppendLine("Machine Name,Operator,Status,City/District,Latitude,Longitude,Last Seen");

        foreach (var item in Items)
            sb.AppendLine($"\"{item.Name}\",\"{item.OperatorName}\",\"{item.Status}\",\"{item.City}\",\"{item.LatitudeRaw}\",\"{item.LongitudeRaw}\",\"{item.LastSeen}\"");

        File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
        MessageBox.Show($"Exported {Items.Count} record(s) successfully.", "Export Complete",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }
}

public sealed class MachineDetailItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public MachineStatus Status       { get; }
    public string        Name         { get; }
    public string        OperatorName  { get; }
    public string        Coordinates  { get; }
    public string        LastSeen     { get; }
    public string        LatitudeRaw  { get; }
    public string        LongitudeRaw { get; }
    public double        Lat          { get; }
    public double        Lon          { get; }
    public bool          HasCoordinates { get; }

    private string _city = "—";
    public string City
    {
        get => _city;
        set { _city = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(City))); }
    }

    public MachineDetailItem(MachineDto m)
    {
        Status         = m.Status;
        Name           = m.Name;
        OperatorName   = m.AssignedOperatorName ?? "Unassigned";
        HasCoordinates = m.CurrentLatitude.HasValue && m.CurrentLongitude.HasValue;
        Lat            = m.CurrentLatitude  ?? 0;
        Lon            = m.CurrentLongitude ?? 0;
        LatitudeRaw    = m.CurrentLatitude?.ToString("F6")  ?? "";
        LongitudeRaw   = m.CurrentLongitude?.ToString("F6") ?? "";
        Coordinates    = m.CurrentLatitude.HasValue
            ? $"Lat: {m.CurrentLatitude:F4}°  |  Lon: {m.CurrentLongitude:F4}°"
            : "No GPS data";
        LastSeen = m.LastSeenAt.HasValue
            ? m.LastSeenAt.Value.ToLocalTime().ToString("dd MMM yyyy HH:mm")
            : "Never";
    }
}
