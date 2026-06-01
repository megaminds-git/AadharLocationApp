using System.Windows;
using System.Windows.Controls;
using AadharLocation.AdminDashboard.ViewModels;
using AadharLocation.AdminDashboard.Views.Dialogs;

namespace AadharLocation.AdminDashboard.Views.Pages;

public partial class SettingsPage : UserControl
{
    private readonly SettingsViewModel _vm;

    public SettingsPage(SettingsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        vm.UninstallCodeGenerated += OnUninstallCodeGenerated;
    }

    public async Task ActivateAsync() => await _vm.LoadAsync();

    private void OnUninstallCodeGenerated(string code)
    {
        var dialog = new UninstallCodeDialog(code) { Owner = Window.GetWindow(this) };
        dialog.ShowDialog();
    }
}
