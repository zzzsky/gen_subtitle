using System.Windows;
using System.Windows.Controls;

namespace GenSubtitle.App.Views;

public partial class TimeAdjustDialog : Window
{
    public string Mode { get; private set; } = "Offset";
    public double Value { get; private set; }

    public TimeAdjustDialog()
    {
        InitializeComponent();
    }

    private void OnApply(object sender, RoutedEventArgs e)
    {
        if (ModeComboBox.SelectedItem is ComboBoxItem item)
        {
            Mode = item.Tag?.ToString() ?? "Offset";
        }

        if (double.TryParse(ValueTextBox.Text, out var value))
        {
            Value = value;
            DialogResult = true;
        }
        else
        {
            MessageBox.Show("请输入有效的数值", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
