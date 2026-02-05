using System.Windows;
using GenSubtitle.Core.Models;

namespace GenSubtitle.App.Views;

public partial class ExportWindow : Window
{
    private readonly ExportOptions _options;

    public ExportWindow(ExportOptions options)
    {
        InitializeComponent();
        _options = options;

        if (options.SoftMux || (!options.BurnIn && !options.SoftMux))
        {
            SoftMuxRadio.IsChecked = true;
        }
        else
        {
            BurnInRadio.IsChecked = true;
        }
        StyleBox.Text = options.AssStyleName;
    }

    public ExportOptions Options => _options;

    private void OnSave(object sender, RoutedEventArgs e)
    {
        _options.BurnIn = BurnInRadio.IsChecked == true;
        _options.SoftMux = SoftMuxRadio.IsChecked == true;
        _options.AssStyleName = StyleBox.Text.Trim();
        if (!_options.BurnIn && !_options.SoftMux)
        {
            MessageBox.Show("Select at least one export option.", "Export", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
