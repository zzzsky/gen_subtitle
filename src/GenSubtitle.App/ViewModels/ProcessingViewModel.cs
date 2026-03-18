using GenSubtitle.App.Services;

namespace GenSubtitle.App.ViewModels;

/// <summary>
/// ViewModel for the processing/task list screen
/// </summary>
public class ProcessingViewModel : ObservableObject
{
    private readonly ITaskQueueService _taskQueue;
    private TaskItemViewModel? _selectedTask;

    public ProcessingViewModel(ITaskQueueService taskQueue)
    {
        _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
        // NO base() call - ObservableObject has parameterless constructor
        EditCommand = new RelayCommand(() => EditSelectedTask());
    }

    public System.Collections.ObjectModel.ObservableCollection<TaskItemViewModel> Tasks => _taskQueue.Tasks;

    public TaskItemViewModel? SelectedTask
    {
        get => _selectedTask;
        set
        {
            if (SetProperty(ref _selectedTask, value))
            {
                _taskQueue.SelectedTask = value;
            }
        }
    }

    public RelayCommand EditCommand { get; }

    private void EditSelectedTask()
    {
        if (SelectedTask != null)
        {
            // TODO: Trigger transition to Editing state (Task 16)
        }
    }
}
