using System.Windows;
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
        vm.UninstallCodeGenerated += OnUninstallCodeGenerated;
    }

    public async Task ActivateAsync() => await _vm.LoadAsync();

    private void OnUninstallCodeGenerated(string code, DateTime expiresAt)
    {
        MessageBox.Show(
            $"Uninstall code: {code}\n\nGive this code to the operator.\nExpires: {expiresAt.ToLocalTime():dd MMM yyyy HH:mm}",
            "Uninstall Code Generated",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
