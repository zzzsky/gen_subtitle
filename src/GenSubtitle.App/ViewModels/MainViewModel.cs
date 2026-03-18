using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GenSubtitle.App.Services;
using GenSubtitle.App.Core;
using GenSubtitle.Core.Models;
using CoreTaskStatus = GenSubtitle.Core.Models.TaskStatus;

namespace GenSubtitle.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly SettingsService _settingsService = new();
    private AppSettings _settings;
    private TaskItemViewModel? _selectedTask;
    private readonly ViewStateManager _stateManager;
    private object _currentView;

    public MainViewModel()
    {
        _settings = _settingsService.Load();
        Queue = new TaskQueueViewModel(_settings);
        Queue.LoadCachedTasks();

        // Create task queue service adapter
        var taskQueueService = new TaskQueueServiceAdapter(Queue);

        // Create state manager
        _stateManager = new ViewStateManager(taskQueueService, new ConsoleLogger());

        // Set initial view
        _currentView = new IdleViewModel(taskQueueService);

        // Subscribe to task queue changes for auto state transitions
        Queue.Tasks.CollectionChanged += (s, e) => OnTasksChanged();
        Queue.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TaskQueueViewModel.SelectedTask))
            {
                OnTaskSelectionChanged();
            }
        };
    }

    private void OnTasksChanged()
    {
        // Auto-transition based on task count
        if (Queue.Tasks.Count == 0 && _stateManager.CurrentState != ViewState.Idle)
        {
            _stateManager.TransitionTo(ViewState.Idle);
            SwitchView(ViewState.Idle);
        }
        else if (Queue.Tasks.Count > 0 && _stateManager.CurrentState == ViewState.Idle)
        {
            _stateManager.TransitionTo(ViewState.Processing);
            SwitchView(ViewState.Processing);
        }
    }

    private void OnTaskSelectionChanged()
    {
        var selectedTask = Queue.SelectedTask;
        var currentState = _stateManager.CurrentState;

        if (selectedTask == null)
        {
            // No task selected
            if (Queue.Tasks.Count == 0)
            {
                _stateManager.TransitionTo(ViewState.Idle);
                SwitchView(ViewState.Idle);
            }
            else if (currentState == ViewState.Editing)
            {
                _stateManager.TransitionTo(ViewState.Processing);
                SwitchView(ViewState.Processing);
            }
        }
        else if (selectedTask.Status == CoreTaskStatus.Completed && currentState != ViewState.Editing)
        {
            // Completed task selected - switch to editing
            _stateManager.TransitionTo(ViewState.Editing);
            SwitchView(ViewState.Editing);
        }
        else if (selectedTask.Status != CoreTaskStatus.Completed && currentState == ViewState.Editing)
        {
            // Non-completed task selected while in editing - switch to processing
            _stateManager.TransitionTo(ViewState.Processing);
            SwitchView(ViewState.Processing);
        }
    }

    public TaskQueueViewModel Queue { get; }

    public ViewStateManager StateManager => _stateManager;

    public object CurrentView
    {
        get => _currentView;
        private set => SetProperty(ref _currentView, value);
    }

    public void SwitchView(ViewState state)
    {
        var taskQueueService = new TaskQueueServiceAdapter(Queue);
        CurrentView = state switch
        {
            ViewState.Idle => new IdleViewModel(taskQueueService),
            ViewState.Processing => new ProcessingViewModel(taskQueueService),
            ViewState.Editing => new EditingViewModel(taskQueueService),
            _ => throw new ArgumentException($"Invalid view state: {state}")
        };
    }

    public AppSettings Settings
    {
        get => _settings;
        set => SetProperty(ref _settings, value);
    }

    public TaskItemViewModel? SelectedTask
    {
        get => _selectedTask;
        set
        {
            if (SetProperty(ref _selectedTask, value))
            {
                Queue.SelectedTask = value;
                RaisePropertyChanged(nameof(SelectedVideoPath));
                RaisePropertyChanged(nameof(SelectedLanguage));
            }
        }
    }

    public string SelectedVideoPath => SelectedTask?.FilePath ?? string.Empty;

    public string SelectedLanguage => SelectedTask?.DetectedLanguage ?? string.Empty;

    public void SaveSettings()
    {
        _settingsService.Save(Settings);
        Queue.UpdateSettings(Settings);
    }

    public void EnqueueFiles(string[] files)
    {
        var first = Queue.EnqueueFiles(files);
        if (SelectedTask is null && first is not null)
        {
            SelectedTask = first;
        }
    }

    public Task ExportTaskAsync(TaskItemViewModel task, ExportOptions options, Action<double>? onProgress = null, CancellationToken cancellationToken = default)
    {
        return Queue.ExportTaskAsync(task, options, onProgress, cancellationToken);
    }

    public void AlignSelectedStart(TimeSpan position)
    {
        if (SelectedTask?.SelectedSegment is null)
        {
            return;
        }
        SelectedTask.SelectedSegment.Start = position;
    }

    public void AlignSelectedEnd(TimeSpan position)
    {
        if (SelectedTask?.SelectedSegment is null)
        {
            return;
        }
        SelectedTask.SelectedSegment.End = position;
    }
}
