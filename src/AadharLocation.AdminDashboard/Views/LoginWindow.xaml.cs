using System.Windows;
using System.Windows.Input;
using AadharLocation.AdminDashboard.ViewModels;
using MaterialDesignThemes.Wpf;

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
        Loaded += (_, _) => SyncThemeIcon();
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
        var password = RevealToggle.IsChecked == true
            ? PasswordText.Text
            : PasswordBox.Password;
        if (_vm.LoginCommand.CanExecute(null))
            _vm.LoginCommand.Execute(password);
    }

    private void RevealToggle_Click(object sender, RoutedEventArgs e)
    {
        if (RevealToggle.IsChecked == true)
        {
            PasswordText.Text = PasswordBox.Password;
            PasswordBox.Visibility = Visibility.Collapsed;
            PasswordText.Visibility = Visibility.Visible;
            PasswordText.CaretIndex = PasswordText.Text.Length;
            RevealIcon.Kind = PackIconKind.EyeOff;
        }
        else
        {
            PasswordBox.Password = PasswordText.Text;
            PasswordText.Visibility = Visibility.Collapsed;
            PasswordBox.Visibility = Visibility.Visible;
            RevealIcon.Kind = PackIconKind.Eye;
        }
    }

    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        App.SwitchTheme(!App.IsDarkTheme);
        SyncThemeIcon();
    }

    private void SyncThemeIcon()
    {
        ThemeToggleIcon.Kind = App.IsDarkTheme
            ? PackIconKind.WeatherSunny   // dark active → click to go light
            : PackIconKind.WeatherNight;  // light active → click to go dark
    }
}
