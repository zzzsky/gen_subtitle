using GenSubtitle.App.Services;

namespace GenSubtitle.App.ViewModels;

/// <summary>
/// ViewModel for the editing/video player screen
/// </summary>
public class EditingViewModel : ObservableObject
{
    private readonly ITaskQueueService _taskQueue;

    public EditingViewModel(ITaskQueueService taskQueue)
    {
        _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
        // NO base() call - ObservableObject has parameterless constructor
        SelectedTask = _taskQueue.SelectedTask;
        ReturnToProcessingCommand = new RelayCommand(ReturnToProcessing);
    }

    public TaskItemViewModel? SelectedTask { get; private set; }

    public RelayCommand ReturnToProcessingCommand { get; }

    public void SetSelectedTask(TaskItemViewModel? task)
    {
        SelectedTask = task;
        OnPropertyChanged(nameof(SelectedTask));
    }

    private void ReturnToProcessing()
    {
        // Deselect current task to trigger state change
        _taskQueue.SelectedTask = null;
    }
}
