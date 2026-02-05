using System;
using System.IO;
using System.Text.Json;
using GenSubtitle.Core.Models;

namespace GenSubtitle.App.Services;

public sealed class SettingsService
{
    private readonly string _settingsPath;

    public SettingsService()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GenSubtitle");
        Directory.CreateDirectory(root);
        _settingsPath = Path.Combine(root, "settings.json");
    }

    public AppSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            var fresh = new AppSettings();
            ApplyBundledPaths(fresh);
            return fresh;
        }

        var json = File.ReadAllText(_settingsPath);
        var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        ApplyBundledPaths(settings);
        return settings;
    }

    public void Save(AppSettings settings)
    {
        ApplyBundledPaths(settings);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsPath, json);
    }

    private static void ApplyBundledPaths(AppSettings settings)
    {
        settings.FfmpegPath = Path.Combine(AppContext.BaseDirectory, "ffmpeg", "x64", "ffmpeg.exe");
        var baseDir = Path.Combine(AppContext.BaseDirectory, "whisper", "x64");
        var cliPath = Path.Combine(baseDir, "whisper-cli.exe");
        var legacyPath = Path.Combine(baseDir, "whisper.exe");
        settings.WhisperPath = File.Exists(cliPath) ? cliPath : legacyPath;
    }
}
