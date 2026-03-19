using System;
using System.Threading;
using System.Threading.Tasks;
using GenSubtitle.Core.Models;

namespace GenSubtitle.App.Services;

/// <summary>
/// Adapts TaskQueueViewModel to ITaskQueueService interface
/// </summary>
public class TaskQueueServiceAdapter : ITaskQueueService
{
    private readonly ViewModels.TaskQueueViewModel _queue;

    public TaskQueueServiceAdapter(ViewModels.TaskQueueViewModel queue)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
    }

    public System.Collections.ObjectModel.ObservableCollection<ViewModels.TaskItemViewModel> Tasks => _queue.Tasks;

    public ViewModels.TaskItemViewModel? SelectedTask
    {
        get => _queue.SelectedTask;
        set => _queue.SelectedTask = value;
    }

    public ViewModels.TaskItemViewModel? EnqueueFiles(string[] files)
    {
        return _queue.EnqueueFiles(files);
    }

    public Task ExportTaskAsync(ViewModels.TaskItemViewModel task, ExportOptions options, Action<double>? onProgress = null, CancellationToken cancellationToken = default)
    {
        return _queue.ExportTaskAsync(task, options, onProgress, cancellationToken);
    }

    public void DeleteTask(ViewModels.TaskItemViewModel task, bool deleteFolder, bool force)
    {
        _queue.DeleteTask(task, deleteFolder, force);
    }

    public void PauseTask(ViewModels.TaskItemViewModel task)
    {
        _queue.PauseTask(task);
    }

    public void ResumeTask(ViewModels.TaskItemViewModel task)
    {
        _queue.ResumeTask(task);
    }

    public void CancelTask(ViewModels.TaskItemViewModel task)
    {
        _queue.CancelTask(task);
    }

    public void OpenFolder(ViewModels.TaskItemViewModel task)
    {
        _queue.OpenFolder(task);
    }

    public bool CanExportSelected => _queue.CanExportSelected;
}
