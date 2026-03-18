using GenSubtitle.App.Services;

namespace GenSubtitle.App.ViewModels;

/// <summary>
/// ViewModel for the processing/task list screen
/// </summary>
public class ProcessingViewModel : ObservableObject
{
    private readonly ITaskQueueService _taskQueue;

    public ProcessingViewModel(ITaskQueueService taskQueue)
    {
        _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
        // NO base() call - ObservableObject has parameterless constructor
    }

    public System.Collections.ObjectModel.ObservableCollection<TaskItemViewModel> Tasks => _taskQueue.Tasks;
}
