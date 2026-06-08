using System.Windows;
using System.Windows.Controls;
using AadharLocation.AdminDashboard.ViewModels;
using AadharLocation.AdminDashboard.Views.Dialogs;
using AadharLocation.Shared.DTOs.Admins;

namespace AadharLocation.AdminDashboard.Views.Pages;

public partial class AdminsPage : UserControl
{
    private readonly AdminsViewModel _vm;
    private readonly AddAdminViewModel _addVm;

    public AdminsPage(AdminsViewModel vm, AddAdminViewModel addVm)
    {
        InitializeComponent();
        _vm    = vm;
        _addVm = addVm;
        DataContext = vm;

        vm.AddRequested  += OnAddRequested;
        vm.EditRequested += OnEditRequested;
        vm.ConfirmDelete  = name =>
        {
            var dlg = new ConfirmDeleteDialog(name) { Owner = Window.GetWindow(this) };
            return dlg.ShowDialog() == true;
        };
    }

    public async Task ActivateAsync() => await _vm.LoadAsync();

    private async void OnAddRequested()
    {
        await _addVm.InitForAddAsync();
        var dialog = new AddAdminDialog(_addVm) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true)
            await _vm.LoadAsync();
    }

    private async void OnEditRequested(AdminDto admin)
    {
        await _addVm.InitForEditAsync(admin);
        var dialog = new AddAdminDialog(_addVm) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true)
            await _vm.LoadAsync();
    }
}
