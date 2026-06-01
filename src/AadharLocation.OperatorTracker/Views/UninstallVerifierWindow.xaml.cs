using AadharLocation.OperatorTracker.ViewModels;
using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Controls;

namespace AadharLocation.OperatorTracker.Views;

public partial class UninstallVerifierWindow : Window
{
    private readonly UninstallVerifierViewModel _vm;

    public UninstallVerifierWindow(UninstallVerifierViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        vm.VerificationSucceeded += (_, _) => { DialogResult = true; };
        vm.Cancelled += (_, _) => { DialogResult = false; };
    }

    private void CodeBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (!_vm.IsCodeVisible)
            _vm.Code = CodeBox.Password;
    }

    private void ToggleCodeButton_Click(object sender, RoutedEventArgs e)
    {
        _vm.IsCodeVisible = !_vm.IsCodeVisible;
        EyeIcon.Kind = _vm.IsCodeVisible ? PackIconKind.EyeOff : PackIconKind.Eye;

        if (_vm.IsCodeVisible)
            CodeText.CaretIndex = CodeText.Text.Length;
        else
            CodeBox.Password = _vm.Code;
    }
}
