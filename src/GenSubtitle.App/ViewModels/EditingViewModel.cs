using System;
using System.Windows.Input;
using GenSubtitle.App.Services;

namespace GenSubtitle.App.ViewModels;

/// <summary>
/// ViewModel for the editing/video player screen
/// </summary>
public class EditingViewModel : ObservableObject
{
    private readonly ITaskQueueService _taskQueue;
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

        // Subscribe to task changes
        _taskQueue.Tasks.CollectionChanged += (s, e) => OnTaskCollectionChanged();

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
        // TODO: Implement save functionality
        // For now, this is a placeholder for the save command
        // In a full implementation, this would save the current subtitle edits
    }
}
