using System.Windows;
using System.Windows.Input;
using AadharLocation.AdminDashboard.ViewModels;

namespace AadharLocation.AdminDashboard.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _vm;

    public LoginWindow(LoginViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        vm.LoginSucceeded += OnLoginSucceeded;
    }

    private void OnLoginSucceeded()
    {
        Close();
    }

    private void Input_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            DoLogin();
    }

    private void SignInButton_Click(object sender, RoutedEventArgs e) => DoLogin();

    private void DoLogin()
    {
        if (_vm.LoginCommand.CanExecute(null))
            _vm.LoginCommand.Execute(PasswordBox.Password);
    }
}
