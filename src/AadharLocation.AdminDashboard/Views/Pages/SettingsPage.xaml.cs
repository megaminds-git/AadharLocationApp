using System.Windows.Controls;
using AadharLocation.AdminDashboard.ViewModels;

namespace AadharLocation.AdminDashboard.Views.Pages;

public partial class SettingsPage : UserControl
{
    private readonly SettingsViewModel _vm;

    public SettingsPage(SettingsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
    }

    public async Task ActivateAsync() => await _vm.LoadAsync();
}
