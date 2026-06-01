using AadharLocation.OperatorTracker.ViewModels;
using System.Windows;

namespace AadharLocation.OperatorTracker.Views;

public partial class LocationAccessWindow : Window
{
    private bool _accessGranted;

    public LocationAccessWindow(LocationAccessViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.AccessGranted += (_, _) =>
        {
            _accessGranted = true;
            Close();
        };
        Closing += (_, e) =>
        {
            if (!_accessGranted)
                System.Windows.Application.Current.Shutdown();
        };
    }
}
