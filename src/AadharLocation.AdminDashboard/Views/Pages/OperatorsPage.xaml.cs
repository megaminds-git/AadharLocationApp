using System.Windows.Controls;
using AadharLocation.AdminDashboard.ViewModels;
using AadharLocation.AdminDashboard.Views.Dialogs;
using AadharLocation.Shared.DTOs.Operators;

namespace AadharLocation.AdminDashboard.Views.Pages;

public partial class OperatorsPage : UserControl
{
    private readonly OperatorsViewModel _vm;
    private readonly AddOperatorViewModel _addVm;

    public OperatorsPage(OperatorsViewModel vm, AddOperatorViewModel addVm)
    {
        InitializeComponent();
        _vm    = vm;
        _addVm = addVm;
        DataContext = vm;

        vm.AddRequested  += OnAddRequested;
        vm.EditRequested += OnEditRequested;
    }

    public async Task ActivateAsync() => await _vm.LoadAsync();

    private async void OnAddRequested()
    {
        await _addVm.InitForAddAsync();
        var dialog = new AddOperatorDialog(_addVm);
        if (dialog.ShowDialog() == true)
            await _vm.LoadAsync();
    }

    private async void OnEditRequested(OperatorDto? op)
    {
        if (op == null) return;
        await _addVm.InitForEditAsync(op);
        var dialog = new AddOperatorDialog(_addVm);
        if (dialog.ShowDialog() == true)
            await _vm.LoadAsync();
    }
}
