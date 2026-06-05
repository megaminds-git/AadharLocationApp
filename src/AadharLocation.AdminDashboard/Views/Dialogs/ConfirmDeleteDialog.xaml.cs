using System.Windows;

namespace AadharLocation.AdminDashboard.Views.Dialogs;

public partial class ConfirmDeleteDialog : Window
{
    public ConfirmDeleteDialog(string itemName)
    {
        InitializeComponent();
        MessageText.Text = $"Are you sure you want to delete \"{itemName}\"? This action cannot be undone.";
    }

    private void YesButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    private void NoButton_Click(object sender, RoutedEventArgs e)  => DialogResult = false;
}
