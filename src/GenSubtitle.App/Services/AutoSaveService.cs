using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using GenSubtitle.App.ViewModels;

namespace GenSubtitle.App.Services;

/// <summary>
/// Service for auto-saving subtitle edits with debouncing
/// </summary>
public class AutoSaveService
{
    private readonly Timer? _timer;
    private readonly string _autoSaveDirectory;
    private readonly object _lock = new();
    private TaskItemViewModel? _currentTask;

    public AutoSaveService(string autoSaveDirectory)
    {
        _autoSaveDirectory = autoSaveDirectory ?? throw new ArgumentNullException(nameof(autoSaveDirectory));
        _timer = new Timer(OnAutoSaveTimer, null, Timeout.Infinite, Timeout.Infinite);

        // Ensure auto-save directory exists
        if (!Directory.Exists(_autoSaveDirectory))
        {
            Directory.CreateDirectory(_autoSaveDirectory);
        }
    }

    /// <summary>
    /// Request an auto-save with debouncing (2 seconds delay)
    /// </summary>
    public void RequestAutoSave(TaskItemViewModel task)
    {
        lock (_lock)
        {
            _currentTask = task;
        }

        // Reset timer to 2 seconds
        _timer?.Change(2000, Timeout.Infinite);
    }

    private void OnAutoSaveTimer(object? state)
    {
        TaskItemViewModel? taskToSave;

        lock (_lock)
        {
            taskToSave = _currentTask;
        }

        if (taskToSave == null)
        {
            return;
        }

        try
        {
            PerformAutoSave(taskToSave);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Auto-save failed: {ex.Message}");
        }
    }

    private void PerformAutoSave(TaskItemViewModel task)
    {
        var autoSavePath = Path.Combine(_autoSaveDirectory, $"task_{task.Id}_autosave.json");

        var autoSaveData = new AutoSaveData
        {
            TaskId = task.Id,
            FileName = task.FileName,
            FilePath = task.FilePath,
            SavedAt = DateTime.UtcNow,
            Segments = task.Segments.Select(s => new SubtitleSegmentData
            {
                Id = s.Id,
                Start = s.Start,
                End = s.End,
                SourceText = s.SourceText,
                ZhText = s.ZhText
            }).ToArray()
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(autoSaveData, options);
        File.WriteAllText(autoSavePath, json);

        Console.WriteLine($"Auto-saved to: {autoSavePath}");
    }

    /// <summary>
    /// Check if there's an auto-saved version for a task
    /// </summary>
    public AutoSaveData? LoadAutoSave(TaskItemViewModel task)
    {
        var autoSavePath = Path.Combine(_autoSaveDirectory, $"task_{task.Id}_autosave.json");
        if (!File.Exists(autoSavePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(autoSavePath);
            var data = JsonSerializer.Deserialize<AutoSaveData>(json);
            return data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load auto-save: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Clear auto-save file for a task
    /// </summary>
    public void ClearAutoSave(TaskItemViewModel task)
    {
        var autoSavePath = Path.Combine(_autoSaveDirectory, $"task_{task.Id}_autosave.json");
        if (File.Exists(autoSavePath))
        {
            File.Delete(autoSavePath);
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}

/// <summary>
/// Auto-save data structure
/// </summary>
public class AutoSaveData
{
    public int TaskId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime SavedAt { get; set; }
    public SubtitleSegmentData[] Segments { get; set; } = Array.Empty<SubtitleSegmentData>();
}

/// <summary>
/// Simplified segment data for serialization
/// </summary>
public class SubtitleSegmentData
{
    public int Id { get; set; }
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public string SourceText { get; set; } = string.Empty;
    public string ZhText { get; set; } = string.Empty;
}
