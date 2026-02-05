using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using GenSubtitle.Core.Models;

namespace GenSubtitle.App.Views;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;

        WhisperModelBox.ItemsSource = new[]
        {
            "small",
            "medium",
            "large-v3"
        };
        WhisperModelBox.SelectedItem = settings.WhisperModel;
        UseGpuBox.IsChecked = settings.UseGpu;
        QwenKeyBox.Text = settings.QwenApiKey;
        QwenModelBox.Text = settings.QwenModel;
        AutoTranslateBox.IsChecked = settings.AutoTranslateEnabled;
        MaxConcurrencyBox.Text = settings.MaxConcurrency.ToString(CultureInfo.InvariantCulture);
        LanguagesBox.Text = string.Join(',', settings.EnabledLanguages);
    }

    public AppSettings UpdatedSettings => _settings;

    private void OnSave(object sender, RoutedEventArgs e)
    {
        _settings.WhisperModel = (WhisperModelBox.SelectedItem as string ?? _settings.WhisperModel).Trim();
        _settings.UseGpu = UseGpuBox.IsChecked ?? false;
        _settings.QwenApiKey = QwenKeyBox.Text.Trim();
        _settings.QwenModel = string.IsNullOrWhiteSpace(QwenModelBox.Text) ? _settings.QwenModel : QwenModelBox.Text.Trim();
        _settings.AutoTranslateEnabled = AutoTranslateBox.IsChecked ?? true;
        if (int.TryParse(MaxConcurrencyBox.Text, out var maxConcurrency) && maxConcurrency > 0)
        {
            _settings.MaxConcurrency = maxConcurrency;
        }
        _settings.EnabledLanguages = LanguagesBox.Text.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
