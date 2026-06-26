using System;
using System.Threading;
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
        for (int i = 0; i < 5; i++)
        {
            try
            {
                Clipboard.SetDataObject(_code, true);
                return;
            }
            catch (Exception)
            {
                Thread.Sleep(15);
            }
        }
        MessageBox.Show("Could not copy to clipboard. Please copy the code manually.", "Copy Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
