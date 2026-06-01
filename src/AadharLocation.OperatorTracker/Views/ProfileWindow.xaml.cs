using AadharLocation.OperatorTracker.ViewModels;
using System.Windows;

namespace AadharLocation.OperatorTracker.Views;

public partial class ProfileWindow : Window
{
    private readonly ProfileViewModel _vm;

    public ProfileWindow(ProfileViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        vm.LogoutRequested += (_, _) => Close();
        Closed += (_, _) => vm.Dispose();
    }

    private async void Window_ContentRendered(object sender, EventArgs e)
        => await _vm.LoadAsync();
}
