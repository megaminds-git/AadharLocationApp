using System.Windows;
using AadharLocation.AdminDashboard.ViewModels;

namespace AadharLocation.AdminDashboard.Views.Dialogs;

public partial class GeofenceEditorDialog : Window
{
    public GeofenceEditorDialog(GeofenceEditorViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.SaveSucceeded += () => { DialogResult = true; Close(); };
    }
}
