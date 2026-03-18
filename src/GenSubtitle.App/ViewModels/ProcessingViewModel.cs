using System;
using System.Linq;
using GenSubtitle.App.Services;
using CoreTaskStatus = GenSubtitle.Core.Models.TaskStatus;

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
        EditCommand = new RelayCommand(EditSelectedTask, CanEditSelectedTask);
        ReturnToIdleCommand = new RelayCommand(ReturnToIdle, CanReturnToIdle);
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
    public RelayCommand ReturnToIdleCommand { get; }

    private bool CanEditSelectedTask()
    {
        return SelectedTask?.Status == CoreTaskStatus.Completed;
    }

    private void EditSelectedTask()
    {
        // Selection already triggers state transition via MainViewModel
        // This button is just for explicit user action
    }

    private bool CanReturnToIdle()
    {
        return _taskQueue.Tasks.Count == 0;
    }

    private void ReturnToIdle()
    {
        // Clear all tasks to return to idle state
        var tasks = _taskQueue.Tasks.ToList();
        foreach (var task in tasks)
        {
            _taskQueue.DeleteTask(task, false, true);
        }
    }
}
