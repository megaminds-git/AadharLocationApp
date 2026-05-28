using AadharLocation.OperatorTracker.ViewModels;
using System.Windows;

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
}
