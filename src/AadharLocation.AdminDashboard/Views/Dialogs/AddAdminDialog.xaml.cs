using System.Windows;
using AadharLocation.AdminDashboard.ViewModels;
using MaterialDesignThemes.Wpf;

namespace AadharLocation.AdminDashboard.Views.Dialogs;

public partial class AddAdminDialog : Window
{
    private readonly AddAdminViewModel _vm;

    public AddAdminDialog(AddAdminViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        void onSaveSucceeded() { DialogResult = true; }
        vm.SaveSucceeded += onSaveSucceeded;
        Closed += (_, _) => vm.SaveSucceeded -= onSaveSucceeded;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _vm.SaveCommand.Execute(null);
    }

    private void PwdRevealToggle_Click(object sender, RoutedEventArgs e)
    {
        if (PwdRevealToggle.IsChecked == true)
        {
            PwdBox.Visibility  = Visibility.Collapsed;
            PwdText.Visibility = Visibility.Visible;
            PwdText.CaretIndex = PwdText.Text.Length;
            PwdRevealIcon.Kind = PackIconKind.EyeOff;
        }
        else
        {
            PwdText.Visibility = Visibility.Collapsed;
            PwdBox.Visibility  = Visibility.Visible;
            PwdRevealIcon.Kind = PackIconKind.Eye;
        }
    }
}
