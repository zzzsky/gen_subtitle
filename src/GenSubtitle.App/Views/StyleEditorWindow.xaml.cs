using System.Windows;
using System.Windows.Controls;

namespace GenSubtitle.App.Views;

public partial class StyleEditorWindow : Window
{
    public string FontFamily { get; private set; } = "Microsoft YaHei";
    public int FontSize { get; private set; } = 24;
    public string FontColor { get; private set; } = "#FFFFFF";
    public string Position { get; private set; } = "BottomCenter";
    public int OutlineWidth { get; private set; } = 2;
    public int BackgroundOpacity { get; private set; } = 50;

    public StyleEditorWindow()
    {
        InitializeComponent();
    }

    private void OnApply(object sender, RoutedEventArgs e)
    {
        // Get values from controls
        if (FontFamilyComboBox.SelectedItem is ComboBoxItem fontItem)
        {
            FontFamily = fontItem.Content.ToString() ?? "Microsoft YaHei";
        }

        FontSize = (int)FontSizeSlider.Value;

        if (FontColorComboBox.SelectedItem is ComboBoxItem colorItem)
        {
            FontColor = colorItem.Tag?.ToString() ?? "#FFFFFF";
        }

        if (PositionComboBox.SelectedItem is ComboBoxItem posItem)
        {
            Position = posItem.Tag?.ToString() ?? "BottomCenter";
        }

        OutlineWidth = (int)OutlineSlider.Value;
        BackgroundOpacity = (int)BackgroundOpacitySlider.Value;

        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void OnSavePreset(object sender, RoutedEventArgs e)
    {
        // TODO: Implement preset saving
        // For now, just show a message
        MessageBox.Show("预设保存功能将在后续版本中实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnPresetSelected(object sender, SelectionChangedEventArgs e)
    {
        if (PresetComboBox.SelectedItem is not ComboBoxItem item) return;

        var preset = item.Content.ToString();
        switch (preset)
        {
            case "默认样式":
                FontSizeSlider.Value = 24;
                FontColorComboBox.SelectedIndex = 0;
                PositionComboBox.SelectedIndex = 0;
                OutlineSlider.Value = 2;
                BackgroundOpacitySlider.Value = 50;
                break;
            case "简洁白色":
                FontSizeSlider.Value = 20;
                FontColorComboBox.SelectedIndex = 0;
                PositionComboBox.SelectedIndex = 0;
                OutlineSlider.Value = 1;
                BackgroundOpacitySlider.Value = 70;
                break;
            case "醒目黄色":
                FontSizeSlider.Value = 28;
                FontColorComboBox.SelectedIndex = 1;
                PositionComboBox.SelectedIndex = 1;
                OutlineSlider.Value = 3;
                BackgroundOpacitySlider.Value = 30;
                break;
            case "电影风格":
                FontSizeSlider.Value = 22;
                FontColorComboBox.SelectedIndex = 0;
                PositionComboBox.SelectedIndex = 2;
                OutlineSlider.Value = 2;
                BackgroundOpacitySlider.Value = 40;
                break;
        }
    }
}
