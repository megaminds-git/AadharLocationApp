using System.Windows;

namespace AadharLocation.AdminDashboard.Views.Dialogs;

public partial class UninstallCodeDialog : Window
{
    private readonly string _code;

    public UninstallCodeDialog(string code)
    {
        InitializeComponent();
        _code = code;
        CodeText.Text = code;
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(_code);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
