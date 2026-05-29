using System.Windows;
using AadharLocation.AdminDashboard.ViewModels;

namespace AadharLocation.AdminDashboard.Views.Dialogs;

public partial class AddOperatorDialog : Window
{
    private readonly AddOperatorViewModel _vm;

    public AddOperatorDialog(AddOperatorViewModel vm)
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
        _vm.TrackerPassword = PwdBox.Password;
        _vm.SaveCommand.Execute(null);
    }
}
