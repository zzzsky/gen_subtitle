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
        var selectedTasks = Tasks.Where(t => t.IsSelected && t.Status == CoreTaskStatus.Completed).ToList();
        if (selectedTasks.Count == 0)
        {
            return;
        }

        // Show dialog
        var dialog = new Views.TimeAdjustDialog();
        var result = dialog.ShowDialog();

        if (result != true)
        {
            return; // User cancelled
        }

        var mode = dialog.Mode;
        var value = dialog.Value;

        // Apply time adjustment to each selected task
        foreach (var task in selectedTasks)
        {
            ApplyTimeAdjustment(task, mode, value);
        }
    }

    private void ApplyTimeAdjustment(TaskItemViewModel task, string mode, double value)
    {
        foreach (var segment in task.Segments)
        {
            if (mode == "Offset")
            {
                // Apply time offset (in seconds)
                segment.Start = TimeSpan.FromSeconds(segment.Start.TotalSeconds + value);
                segment.End = TimeSpan.FromSeconds(segment.End.TotalSeconds + value);

                // Ensure times don't go negative
                if (segment.Start < TimeSpan.Zero)
                {
                    var offset = -segment.Start.TotalSeconds;
                    segment.Start = TimeSpan.Zero;
                    segment.End = TimeSpan.FromSeconds(segment.End.TotalSeconds + offset);
                }
            }
            else if (mode == "Scale")
            {
                // Apply proportional scaling
                var scaleFactor = 1.0 + (value / 100.0); // value is percentage
                var duration = segment.End - segment.Start;
                var newDuration = TimeSpan.FromSeconds(duration.TotalSeconds * scaleFactor);

                segment.End = segment.Start + newDuration;
            }
        }
    }

    private bool CanMergeOverlapping()
    {
        return SelectedTask?.Status == CoreTaskStatus.Completed;
    }

    private void MergeOverlapping()
    {
        if (SelectedTask == null) return;

        var segments = SelectedTask.Segments.OrderBy(s => s.Start).ToList();
        if (segments.Count < 2) return;

        var merged = new List<ViewModels.SubtitleSegmentViewModel>();
        var current = segments[0];

        for (int i = 1; i < segments.Count; i++)
        {
            var next = segments[i];

            // Check if segments overlap (with 0.5 second tolerance)
            if (next.Start < current.End.Add(TimeSpan.FromSeconds(0.5)))
            {
                // Merge: combine text and extend end time
                var mergedSourceText = $"{current.SourceText} {next.SourceText}";
                var mergedZhText = $"{current.ZhText} {next.ZhText}";

                // Create a new merged segment
                // We need to create it using the underlying model and wrap it in a ViewModel
                var mergedSegment = new ViewModels.SubtitleSegmentViewModel(new SubtitleSegment
                {
                    Start = current.Start,
                    End = next.End > current.End ? next.End : current.End,
                    SourceText = mergedSourceText.Trim(),
                    ZhText = mergedZhText.Trim()
                });

                current = mergedSegment;
            }
            else
            {
                // No overlap, add current and move to next
                merged.Add(current);
                current = next;
            }
        }

        // Add the last segment
        merged.Add(current);

        // Update the collection
        SelectedTask.Segments.Clear();
        foreach (var segment in merged)
        {
            SelectedTask.Segments.Add(segment);
        }
    }

    private bool CanSplitLong()
    {
        return SelectedTask?.Status == CoreTaskStatus.Completed;
    }

    private void SplitLong()
    {
        if (SelectedTask == null) return;

        const double maxDurationSeconds = 10.0; // Maximum segment duration in seconds
        var segmentsToReplace = new List<(ViewModels.SubtitleSegmentViewModel original, List<ViewModels.SubtitleSegmentViewModel> replacements)>();

        foreach (var segment in SelectedTask.Segments)
        {
            var duration = (segment.End - segment.Start).TotalSeconds;

            if (duration > maxDurationSeconds)
            {
                // Find split points in source text (sentences)
                var sourceSentences = SplitIntoSentences(segment.SourceText);
                var zhSentences = SplitIntoSentences(segment.ZhText);

                // If we can split into multiple parts
                if (sourceSentences.Count > 1)
                {
                    var replacements = new List<ViewModels.SubtitleSegmentViewModel>();
                    var timePerPart = duration / sourceSentences.Count;
                    var currentTime = segment.Start;

                    for (int i = 0; i < sourceSentences.Count; i++)
                    {
                        var partStart = currentTime;
                        var partEnd = currentTime.Add(TimeSpan.FromSeconds(timePerPart));

                        // Last part takes any remaining time
                        if (i == sourceSentences.Count - 1)
                        {
                            partEnd = segment.End;
                        }

                        var newSegment = new ViewModels.SubtitleSegmentViewModel(new SubtitleSegment
                        {
                            Start = partStart,
                            End = partEnd,
                            SourceText = sourceSentences[i].Trim(),
                            ZhText = i < zhSentences.Count ? zhSentences[i].Trim() : segment.ZhText
                        });

                        replacements.Add(newSegment);
                        currentTime = partEnd;
                    }

                    segmentsToReplace.Add((segment, replacements));
                }
            }
        }

        // Apply replacements
        if (segmentsToReplace.Count > 0)
        {
            var newSegments = new List<ViewModels.SubtitleSegmentViewModel>();

            foreach (var segment in SelectedTask.Segments)
            {
                var replacement = segmentsToReplace.FirstOrDefault(r => r.original == segment);
                if (replacement.original != null)
                {
                    newSegments.AddRange(replacement.replacements);
                }
                else
                {
                    newSegments.Add(segment);
                }
            }

            // Update the collection
            SelectedTask.Segments.Clear();
            foreach (var segment in newSegments)
            {
                SelectedTask.Segments.Add(segment);
            }
        }
    }

    private List<string> SplitIntoSentences(string text)
    {
        var sentences = new List<string>();
        var current = new System.Text.StringBuilder();

        for (int i = 0; i < text.Length; i++)
        {
            current.Append(text[i]);

            // Check for sentence-ending punctuation
            if (text[i] == '。'|| text[i] == '！' || text[i] == '？' ||
                text[i] == '.' || text[i] == '!' || text[i] == '?')
            {
                sentences.Add(current.ToString());
                current.Clear();
            }
            else if (text[i] == '\n' || text[i] == '\r')
            {
                // Treat line breaks as sentence boundaries
                if (current.Length > 0)
                {
                    sentences.Add(current.ToString().Trim());
                    current.Clear();
                }
            }
        }

        // Add any remaining text
        if (current.Length > 0)
        {
            sentences.Add(current.ToString());
        }

        return sentences.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
    }
}
