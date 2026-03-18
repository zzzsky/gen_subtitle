using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using GenSubtitle.App.ViewModels;
using GenSubtitle.Core.Models;

namespace GenSubtitle.App.Services;

/// <summary>
/// Service interface for task queue operations.
/// Provides abstraction between ViewModels and TaskQueueViewModel.
/// </summary>
public interface ITaskQueueService
{
    /// <summary>
    /// Collection of all tasks
    /// </summary>
    ObservableCollection<TaskItemViewModel> Tasks { get; }

    /// <summary>
    /// Currently selected task (null if none)
    /// </summary>
    TaskItemViewModel? SelectedTask { get; set; }

    /// <summary>
    /// Enqueue files for processing
    /// </summary>
    TaskItemViewModel? EnqueueFiles(string[] files);

    /// <summary>
    /// Export task with specified options
    /// </summary>
    Task ExportTaskAsync(TaskItemViewModel task, ExportOptions options, Action<double>? onProgress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete task with optional folder deletion
    /// </summary>
    void DeleteTask(TaskItemViewModel task, bool deleteFolder, bool force);

    /// <summary>
    /// Pause a running task
    /// </summary>
    void PauseTask(TaskItemViewModel task);

    /// <summary>
    /// Resume a paused task
    /// </summary>
    void ResumeTask(TaskItemViewModel task);

    /// <summary>
    /// Cancel a task
    /// </summary>
    void CancelTask(TaskItemViewModel task);

    /// <summary>
    /// Open the output folder for a task
    /// </summary>
    void OpenFolder(TaskItemViewModel task);

    /// <summary>
    /// Check if selected task can be exported
    /// </summary>
    bool CanExportSelected { get; }
}
