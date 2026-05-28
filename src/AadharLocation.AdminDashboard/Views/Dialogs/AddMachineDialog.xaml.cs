using System.Windows;
using AadharLocation.AdminDashboard.ViewModels;

namespace AadharLocation.AdminDashboard.Views.Dialogs;

public partial class AddMachineDialog : Window
{
    public AddMachineDialog(AddMachineViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.SaveSucceeded += () => { DialogResult = true; Close(); };
    }
}
