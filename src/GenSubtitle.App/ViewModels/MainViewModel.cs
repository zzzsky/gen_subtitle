using System;
using System.Threading;
using System.Threading.Tasks;
using GenSubtitle.App.Services;
using GenSubtitle.Core.Models;

namespace GenSubtitle.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly SettingsService _settingsService = new();
    private AppSettings _settings;
    private TaskItemViewModel? _selectedTask;

    public MainViewModel()
    {
        _settings = _settingsService.Load();
        Queue = new TaskQueueViewModel(_settings);
        Queue.LoadCachedTasks();
    }

    public TaskQueueViewModel Queue { get; }

    public AppSettings Settings
    {
        get => _settings;
        set => SetProperty(ref _settings, value);
    }

    public TaskItemViewModel? SelectedTask
    {
        get => _selectedTask;
        set
        {
            if (SetProperty(ref _selectedTask, value))
            {
                Queue.SelectedTask = value;
                RaisePropertyChanged(nameof(SelectedVideoPath));
                RaisePropertyChanged(nameof(SelectedLanguage));
            }
        }
    }

    public string SelectedVideoPath => SelectedTask?.FilePath ?? string.Empty;

    public string SelectedLanguage => SelectedTask?.DetectedLanguage ?? string.Empty;

    public void SaveSettings()
    {
        _settingsService.Save(Settings);
        Queue.UpdateSettings(Settings);
    }

    public void EnqueueFiles(string[] files)
    {
        var first = Queue.EnqueueFiles(files);
        if (SelectedTask is null && first is not null)
        {
            SelectedTask = first;
        }
    }

    public Task ExportTaskAsync(TaskItemViewModel task, ExportOptions options, Action<double>? onProgress = null, CancellationToken cancellationToken = default)
    {
        return Queue.ExportTaskAsync(task, options, onProgress, cancellationToken);
    }

    public void AlignSelectedStart(TimeSpan position)
    {
        if (SelectedTask?.SelectedSegment is null)
        {
            return;
        }
        SelectedTask.SelectedSegment.Start = position;
    }

    public void AlignSelectedEnd(TimeSpan position)
    {
        if (SelectedTask?.SelectedSegment is null)
        {
            return;
        }
        SelectedTask.SelectedSegment.End = position;
    }
}
