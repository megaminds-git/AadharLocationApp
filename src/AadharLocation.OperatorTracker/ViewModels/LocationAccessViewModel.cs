using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using Windows.Devices.Geolocation;

namespace AadharLocation.OperatorTracker.ViewModels;

public partial class LocationAccessViewModel : ObservableObject
{
    public event EventHandler? AccessGranted;

    [ObservableProperty] private bool _isChecking;
    [ObservableProperty] private string _statusMessage = "Location Services must be enabled for this app to work.";

    [RelayCommand]
    private void OpenSettings()
    {
        Process.Start(new ProcessStartInfo("ms-settings:privacy-location") { UseShellExecute = true });
    }

    [RelayCommand(CanExecute = nameof(CanRetry))]
    private async Task RetryAsync()
    {
        IsChecking = true;
        StatusMessage = "Checking...";
        RetryCommand.NotifyCanExecuteChanged();

        try
        {
            var status = await Geolocator.RequestAccessAsync();
            if (status != GeolocationAccessStatus.Denied)
            {
                AccessGranted?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                StatusMessage = "Location access still denied. Please enable it in Windows Settings and try again.";
            }
        }
        finally
        {
            IsChecking = false;
            RetryCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanRetry() => !IsChecking;
}
