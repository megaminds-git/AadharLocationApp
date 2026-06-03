using System.Windows;
using AadharLocation.AdminDashboard.ViewModels;
using MaterialDesignThemes.Wpf;

namespace AadharLocation.AdminDashboard.Views.Dialogs;

public partial class ChangePasswordDialog : Window
{
    public ChangePasswordDialog(ChangePasswordViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;

        void onCloseRequested() => Dispatcher.Invoke(() => { DialogResult = true; });
        vm.CloseRequested += onCloseRequested;
        Closed += (_, _) => vm.CloseRequested -= onCloseRequested;
    }

    private void CurrentPwdToggle_Click(object sender, RoutedEventArgs e)
        => ToggleReveal(CurrentPwdToggle.IsChecked == true,
                        CurrentPwdBox, CurrentPwdText, CurrentPwdIcon);

    private void NewPwdToggle_Click(object sender, RoutedEventArgs e)
        => ToggleReveal(NewPwdToggle.IsChecked == true,
                        NewPwdBox, NewPwdText, NewPwdIcon);

    private void ConfirmPwdToggle_Click(object sender, RoutedEventArgs e)
        => ToggleReveal(ConfirmPwdToggle.IsChecked == true,
                        ConfirmPwdBox, ConfirmPwdText, ConfirmPwdIcon);

    private static void ToggleReveal(bool show,
        System.Windows.Controls.PasswordBox pwdBox,
        System.Windows.Controls.TextBox txtBox,
        PackIcon icon)
    {
        if (show)
        {
            pwdBox.Visibility = Visibility.Collapsed;
            txtBox.Visibility = Visibility.Visible;
            txtBox.CaretIndex = txtBox.Text.Length;
            icon.Kind = PackIconKind.EyeOff;
        }
        else
        {
            txtBox.Visibility = Visibility.Collapsed;
            pwdBox.Visibility = Visibility.Visible;
            icon.Kind = PackIconKind.Eye;
        }
    }
}
