using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Windows;
using AadharLocation.Shared.DTOs.Machines;
using AadharLocation.Shared.Enums;
using Microsoft.Win32;

namespace AadharLocation.AdminDashboard.Views.Dialogs;

public partial class MachineStatusDetailDialog : Window
{
    public List<MachineDetailItem> Items { get; }

    private readonly IHttpClientFactory? _httpFactory;

    public MachineStatusDetailDialog(string title, IEnumerable<MachineDto> machines, IHttpClientFactory? httpFactory = null)
    {
        Items        = machines.Select(m => new MachineDetailItem(m)).ToList();
        _httpFactory = httpFactory;
        InitializeComponent();
        Title       = title;
        DataContext = this;
        Loaded     += async (_, _) => await LoadCitiesAsync();
    }

    private async Task LoadCitiesAsync()
    {
        if (_httpFactory == null) return;

        foreach (var item in Items.Where(i => i.HasCoordinates))
        {
            try
            {
                var http = _httpFactory.CreateClient();
                http.DefaultRequestHeaders.UserAgent.ParseAdd("AadharLocationApp/1.0");
                var url  = $"https://nominatim.openstreetmap.org/reverse?lat={item.Lat.ToString(CultureInfo.InvariantCulture)}&lon={item.Lon.ToString(CultureInfo.InvariantCulture)}&format=json";
                var json = await http.GetStringAsync(url);
                var result = JsonSerializer.Deserialize<NominatimResult>(json);
                var city   = ExtractCity(result?.Address);
                if (!string.IsNullOrEmpty(city))
                    item.City = city;
            }
            catch { /* silently skip if geocoding fails */ }

            await Task.Delay(1100); // Nominatim: max 1 req/sec
        }
    }

    private static string ExtractCity(NominatimAddress? a) =>
        a?.City ?? a?.Town ?? a?.Municipality ?? a?.CityDistrict ?? a?.County ?? a?.Village ?? a?.Suburb ?? string.Empty;

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            FileName  = $"{Title.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmm}",
            DefaultExt = ".csv",
            Filter    = "CSV files (*.csv)|*.csv"
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

    private record NominatimResult(
        [property: JsonPropertyName("address")] NominatimAddress? Address);

    private record NominatimAddress(
        [property: JsonPropertyName("city")]          string? City,
        [property: JsonPropertyName("town")]          string? Town,
        [property: JsonPropertyName("municipality")]  string? Municipality,
        [property: JsonPropertyName("city_district")] string? CityDistrict,
        [property: JsonPropertyName("county")]        string? County,
        [property: JsonPropertyName("village")]       string? Village,
        [property: JsonPropertyName("suburb")]        string? Suburb);
}

public sealed class MachineDetailItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public MachineStatus Status      { get; }
    public string        Name        { get; }
    public string        OperatorName { get; }
    public string        Coordinates { get; }
    public string        LastSeen    { get; }
    public string        LatitudeRaw { get; }
    public string        LongitudeRaw { get; }
    public double        Lat         { get; }
    public double        Lon         { get; }
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
