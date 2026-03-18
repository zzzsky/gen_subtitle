using System;
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

    public event EventHandler<string>? LoadVideoRequested;

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
}
