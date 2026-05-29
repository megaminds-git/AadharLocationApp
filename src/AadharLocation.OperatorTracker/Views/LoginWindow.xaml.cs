using AadharLocation.OperatorTracker.ViewModels;
using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Controls;

namespace AadharLocation.OperatorTracker.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _vm;

    public LoginWindow(LoginViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        vm.LoginSucceeded += (_, _) => Close();
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _vm.Password = PasswordBox.Password;
    }

    private void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        _vm.IsPasswordVisible = !_vm.IsPasswordVisible;
        EyeIcon.Kind = _vm.IsPasswordVisible ? PackIconKind.EyeOff : PackIconKind.Eye;

        if (_vm.IsPasswordVisible)
            PasswordText.CaretIndex = PasswordText.Text.Length;
        else
            PasswordBox.Password = _vm.Password;
    }
}
