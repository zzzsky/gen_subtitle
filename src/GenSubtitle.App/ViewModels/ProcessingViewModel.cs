using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using GenSubtitle.App.Services;
using GenSubtitle.Core.Models;
using CoreTaskStatus = GenSubtitle.Core.Models.TaskStatus;

namespace GenSubtitle.App.ViewModels;

/// <summary>
/// ViewModel for the processing/task list screen
/// </summary>
public class ProcessingViewModel : ObservableObject
{
    private readonly ITaskQueueService _taskQueue;
    private TaskItemViewModel? _selectedTask;
    private bool _selectAllChecked;
    private string _searchText = string.Empty;
    private string _statusFilter = "All";
    private string _dateFilter = "All";
    private ICollectionView? _tasksView;

    public ProcessingViewModel(ITaskQueueService taskQueue)
    {
        _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
        // NO base() call - ObservableObject has parameterless constructor
        EditCommand = new RelayCommand(EditSelectedTask, CanEditSelectedTask);
        ReturnToIdleCommand = new RelayCommand(ReturnToIdle, CanReturnToIdle);
        SelectAllCommand = new RelayCommand(SelectAll, CanSelectAll);
        BatchExportCommand = new RelayCommand(BatchExport, CanBatchExport);
        BatchDeleteCommand = new RelayCommand(BatchDelete, CanBatchDelete);
        ClearCompletedCommand = new RelayCommand(ClearCompleted, CanClearCompleted);
        DeleteSelectedCommand = new RelayCommand(DeleteSelected, CanDeleteSelected);
        ClearSelectionCommand = new RelayCommand(ClearSelection, CanClearSelection);
        BatchTimeAdjustCommand = new RelayCommand(BatchTimeAdjust, CanBatchTimeAdjust);
        MergeOverlappingCommand = new RelayCommand(MergeOverlapping, CanMergeOverlapping);
        SplitLongCommand = new RelayCommand(SplitLong, CanSplitLong);

        // Setup filtered view
        _tasksView = new CollectionViewSource { Source = _taskQueue.Tasks }.View;
        _tasksView.Filter = FilterTasks;

        // Subscribe to collection changes to update selected count and statistics
        _taskQueue.Tasks.CollectionChanged += (s, e) =>
        {
            RaisePropertyChanged(nameof(SelectedTasksCount));
            RaisePropertyChanged(nameof(SelectedTasksText));
            RaisePropertyChanged(nameof(TotalCount));
            RaisePropertyChanged(nameof(CompletedCount));
            RaisePropertyChanged(nameof(FailedCount));
            RaisePropertyChanged(nameof(ProcessingCount));
            RaisePropertyChanged(nameof(QueuedCount));
            RaisePropertyChanged(nameof(SuccessRate));
            RaisePropertyChanged(nameof(FailureRate));
            RaisePropertyChanged(nameof(StatisticsSummary));
            _tasksView?.Refresh();
        };
    }

    public ICollectionView TasksView => _tasksView ?? throw new InvalidOperationException("TasksView not initialized");

    public System.Collections.ObjectModel.ObservableCollection<TaskItemViewModel> Tasks => _taskQueue.Tasks;

    public TaskItemViewModel? SelectedTask
    {
        get => _selectedTask;
        set
        {
            if (SetProperty(ref _selectedTask, value))
            {
                _taskQueue.SelectedTask = value;
                RaisePropertyChanged(nameof(SelectedTasksCount));
                RaisePropertyChanged(nameof(SelectedTasksText));
            }
        }
    }

    public bool SelectAllChecked
    {
        get => _selectAllChecked;
        set
        {
            if (SetProperty(ref _selectAllChecked, value))
            {
                SelectAll();
            }
        }
    }

    public int SelectedTasksCount => Tasks.Count(t => t.IsSelected);

    public string SelectedTasksText => SelectedTasksCount > 0
        ? $"已选 {SelectedTasksCount} 个任务"
        : string.Empty;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _tasksView?.Refresh();
            }
        }
    }

    public string StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetProperty(ref _statusFilter, value))
            {
                _tasksView?.Refresh();
            }
        }
    }

    public string DateFilter
    {
        get => _dateFilter;
        set
        {
            if (SetProperty(ref _dateFilter, value))
            {
                _tasksView?.Refresh();
            }
        }
    }

    // Statistics properties
    public int TotalCount => Tasks.Count;
    public int CompletedCount => Tasks.Count(t => t.Status == CoreTaskStatus.Completed);
    public int FailedCount => Tasks.Count(t => t.Status == CoreTaskStatus.Failed);
    public int ProcessingCount => Tasks.Count(t => t.Status == CoreTaskStatus.Transcribing || t.Status == CoreTaskStatus.Translating);
    public int QueuedCount => Tasks.Count(t => t.Status == CoreTaskStatus.Queued);

    public string SuccessRate => TotalCount > 0
        ? $"{(CompletedCount * 100 / TotalCount)}%"
        : "0%";

    public string FailureRate => TotalCount > 0
        ? $"{(FailedCount * 100 / TotalCount)}%"
        : "0%";

    public string StatisticsSummary => $"总数: {TotalCount} | 已完成: {CompletedCount} | 处理中: {ProcessingCount} | 队列中: {QueuedCount} | 失败: {FailedCount}";

    public RelayCommand EditCommand { get; }
    public RelayCommand ReturnToIdleCommand { get; }
    public RelayCommand SelectAllCommand { get; }
    public RelayCommand BatchExportCommand { get; }
    public RelayCommand BatchDeleteCommand { get; }
    public RelayCommand ClearCompletedCommand { get; }
    public RelayCommand DeleteSelectedCommand { get; }
    public RelayCommand ClearSelectionCommand { get; }
    public RelayCommand BatchTimeAdjustCommand { get; }
    public RelayCommand MergeOverlappingCommand { get; }
    public RelayCommand SplitLongCommand { get; }

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

    private bool CanSelectAll()
    {
        return Tasks.Count > 0;
    }

    private void SelectAll()
    {
        var newState = !SelectAllChecked;
        foreach (var task in Tasks)
        {
            task.IsSelected = newState;
        }
        SelectAllChecked = newState;
        RaisePropertyChanged(nameof(SelectedTasksCount));
        RaisePropertyChanged(nameof(SelectedTasksText));
    }

    private bool CanBatchExport()
    {
        return Tasks.Any(t => t.IsSelected && t.Status == CoreTaskStatus.Completed);
    }

    private async void BatchExport()
    {
        var selectedTasks = Tasks.Where(t => t.IsSelected && t.Status == CoreTaskStatus.Completed).ToList();
        if (selectedTasks.Count == 0)
        {
            return;
        }

        // TODO: Implement batch export with progress dialog
        foreach (var task in selectedTasks)
        {
            // For now, just export with default options
            var options = new ExportOptions();
            await _taskQueue.ExportTaskAsync(task, options);
        }
    }

    private bool CanBatchDelete()
    {
        return Tasks.Any(t => t.IsSelected);
    }

    private void BatchDelete()
    {
        var selectedTasks = Tasks.Where(t => t.IsSelected).ToList();
        foreach (var task in selectedTasks)
        {
            _taskQueue.DeleteTask(task, true, true);
        }
        SelectAllChecked = false;
        RaisePropertyChanged(nameof(SelectedTasksCount));
        RaisePropertyChanged(nameof(SelectedTasksText));
    }

    private bool CanClearCompleted()
    {
        return Tasks.Any(t => t.Status == CoreTaskStatus.Completed);
    }

    private void ClearCompleted()
    {
        var completedTasks = Tasks.Where(t => t.Status == CoreTaskStatus.Completed).ToList();
        foreach (var task in completedTasks)
        {
            _taskQueue.DeleteTask(task, true, true);
        }
    }

    private bool CanDeleteSelected()
    {
        return SelectedTask != null;
    }

    private void DeleteSelected()
    {
        if (SelectedTask != null)
        {
            _taskQueue.DeleteTask(SelectedTask, true, true);
            SelectedTask = null;
        }
    }

    private bool CanClearSelection()
    {
        return SelectAllChecked || SelectedTasksCount > 0;
    }

    private void ClearSelection()
    {
        foreach (var task in Tasks)
        {
            task.IsSelected = false;
        }
        SelectAllChecked = false;
        RaisePropertyChanged(nameof(SelectedTasksCount));
        RaisePropertyChanged(nameof(SelectedTasksText));
    }

    private bool FilterTasks(object obj)
    {
        if (obj is not TaskItemViewModel task)
        {
            return false;
        }

        // Search filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            if (!task.FileName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Status filter
        if (StatusFilter != "All")
        {
            bool matches = StatusFilter switch
            {
                "Completed" => task.Status == CoreTaskStatus.Completed,
                "Processing" => task.Status == CoreTaskStatus.Transcribing || task.Status == CoreTaskStatus.Translating,
                "Failed" => task.Status == CoreTaskStatus.Failed,
                "Queued" => task.Status == CoreTaskStatus.Queued,
                _ => true
            };
            if (!matches)
            {
                return false;
            }
        }

        // Date filter (based on task completion or creation time)
        if (DateFilter != "All")
        {
            // For now, skip date filtering as TaskItemViewModel doesn't have creation date
            // This can be implemented later by adding CreatedAt property
        }

        return true;
    }

    private bool CanBatchTimeAdjust()
    {
        return SelectedTasksCount > 0;
    }

    private async void BatchTimeAdjust()
    {
        // TODO: Show TimeAdjustDialog and apply adjustments
        // For now, this is a placeholder
        // In a full implementation, this would:
        // 1. Show the dialog to get adjustment mode and value
        // 2. Apply time offset or scaling to selected tasks' subtitles
        // 3. Save the modified subtitles
    }

    private bool CanMergeOverlapping()
    {
        return SelectedTask?.Status == CoreTaskStatus.Completed;
    }

    private void MergeOverlapping()
    {
        if (SelectedTask == null) return;

        // TODO: Implement merge overlapping algorithm
        // For now, this is a placeholder
        // In a full implementation, this would:
        // 1. Find segments with overlapping time ranges
        // 2. Merge them into single segments
        // 3. Update the Segments collection
    }

    private bool CanSplitLong()
    {
        return SelectedTask?.Status == CoreTaskStatus.Completed;
    }

    private void SplitLong()
    {
        if (SelectedTask == null) return;

        // TODO: Implement split long sentences feature
        // For now, this is a placeholder
        // In a full implementation, this would:
        // 1. Find segments longer than a threshold (e.g., 10 seconds)
        // 2. Split them at appropriate break points (punctuation, pauses)
        // 3. Update the Segments collection
    }
}
