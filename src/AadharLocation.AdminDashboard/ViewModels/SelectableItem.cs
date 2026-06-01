using CommunityToolkit.Mvvm.ComponentModel;

namespace AadharLocation.AdminDashboard.ViewModels;

public partial class SelectableItem : ObservableObject
{
    [ObservableProperty] private bool _isSelected;

    public int Id { get; }
    public string Label { get; }

    public SelectableItem(int id, string label)
    {
        Id    = id;
        Label = label;
    }
}
