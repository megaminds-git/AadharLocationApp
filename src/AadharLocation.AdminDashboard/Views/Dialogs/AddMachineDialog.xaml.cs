using System.Windows;
using AadharLocation.AdminDashboard.ViewModels;

namespace AadharLocation.AdminDashboard.Views.Dialogs;

public partial class AddMachineDialog : Window
{
    public AddMachineDialog(AddMachineViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;

        void onSaveSucceeded() { DialogResult = true; }
        vm.SaveSucceeded += onSaveSucceeded;
        Closed += (_, _) => vm.SaveSucceeded -= onSaveSucceeded;
    }
}
