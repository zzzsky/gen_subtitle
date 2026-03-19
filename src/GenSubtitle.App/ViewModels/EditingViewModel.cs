using System;
using System.IO;
using System.Windows.Input;
using GenSubtitle.App.Services;
using GenSubtitle.Core.Models;

namespace GenSubtitle.App.ViewModels;

/// <summary>
/// ViewModel for the editing/video player screen
/// </summary>
public class EditingViewModel : ObservableObject
{
    private readonly ITaskQueueService _taskQueue;
    private readonly AutoSaveService _autoSaveService;
    private string? _currentVideoPath;

    public EditingViewModel(ITaskQueueService taskQueue)
    {
        _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
        // NO base() call - ObservableObject has parameterless constructor
        SelectedTask = _taskQueue.SelectedTask;
        ReturnToProcessingCommand = new RelayCommand(ReturnToProcessing);
        PlayPauseCommand = new RelayCommand(PlayPause);
        NudgeBackwardCommand = new RelayCommand(NudgeBackward);
        NudgeForwardCommand = new RelayCommand(NudgeForward);
        SaveCommand = new RelayCommand(Save, CanSave);
        ReTranslateSelectedCommand = new RelayCommand(ReTranslateSelected, CanReTranslateSelected);
        ReTranslateAllCommand = new RelayCommand(ReTranslateAll, CanReTranslateAll);

        // Initialize auto-save service with a common auto-save directory
        var autoSaveDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GenSubtitle", "AutoSave");
        _autoSaveService = new AutoSaveService(autoSaveDir);

        // Subscribe to task changes
        _taskQueue.Tasks.CollectionChanged += (s, e) => OnTaskCollectionChanged();

        // Subscribe to property changes for auto-save
        if (SelectedTask != null)
        {
            SelectedTask.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TaskItemViewModel.Segments))
                {
                    RequestAutoSave();
                }
            };
        }

        // Load initial video if task exists
        if (_taskQueue.SelectedTask is not null)
        {
            CurrentVideoPath = _taskQueue.SelectedTask.FilePath;
        }
    }

    public TaskItemViewModel? SelectedTask { get; private set; }

    public string? CurrentVideoPath
    {
        get => _currentVideoPath;
        private set => SetProperty(ref _currentVideoPath, value);
    }

    public RelayCommand ReturnToProcessingCommand { get; }
    public RelayCommand PlayPauseCommand { get; }
    public RelayCommand NudgeBackwardCommand { get; }
    public RelayCommand NudgeForwardCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand ReTranslateSelectedCommand { get; }
    public RelayCommand ReTranslateAllCommand { get; }

    public event EventHandler<string>? LoadVideoRequested;
    public event EventHandler? PlayPauseRequested;
    public event EventHandler<TimeSpan>? NudgeRequested;

    public void SetSelectedTask(TaskItemViewModel? task)
    {
        SelectedTask = task;
        RaisePropertyChanged(nameof(SelectedTask));
    }

    private void OnTaskCollectionChanged()
    {
        if (_taskQueue.SelectedTask is not null)
        {
            CurrentVideoPath = _taskQueue.SelectedTask.FilePath;
            LoadVideoRequested?.Invoke(this, CurrentVideoPath);
        }
    }

    private void ReturnToProcessing()
    {
        // Deselect current task to trigger state change
        _taskQueue.SelectedTask = null;
    }

    private void PlayPause()
    {
        PlayPauseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void NudgeBackward()
    {
        NudgeRequested?.Invoke(this, TimeSpan.FromSeconds(-0.1));
    }

    private void NudgeForward()
    {
        NudgeRequested?.Invoke(this, TimeSpan.FromSeconds(0.1));
    }

    private bool CanSave()
    {
        return SelectedTask != null;
    }

    private void Save()
    {
        // Manual save - clear auto-save after saving
        if (SelectedTask != null)
        {
            _autoSaveService.ClearAutoSave(SelectedTask);
            // TODO: Implement actual save to SRT file
        }
    }

    private void RequestAutoSave()
    {
        if (SelectedTask != null)
        {
            _autoSaveService.RequestAutoSave(SelectedTask);
        }
    }

    /// <summary>
    /// Check for auto-saved data and load if available
    /// </summary>
    public AutoSaveData? CheckAutoSave()
    {
        if (SelectedTask != null)
        {
            return _autoSaveService.LoadAutoSave(SelectedTask);
        }
        return null;
    }

    private bool CanReTranslateSelected()
    {
        return SelectedTask?.SelectedSegment is not null;
    }

    private async void ReTranslateSelected()
    {
        if (SelectedTask?.SelectedSegment is null) return;

        // TODO: Implement re-translation for selected segment
        // In a full implementation, this would:
        // 1. Call translation service for the selected segment
        // 2. Update the segment's ZhText
        // 3. Save to translation memory
    }

    private bool CanReTranslateAll()
    {
        return SelectedTask != null;
    }

    private async void ReTranslateAll()
    {
        if (SelectedTask == null) return;

        // TODO: Implement re-translation for all segments
        // In a full implementation, this would:
        // 1. Call translation service for all segments
        // 2. Update each segment's ZhText
        // 3. Show progress dialog
        // 4. Save translation memory
    }
}
