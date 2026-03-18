using System;
using GenSubtitle.App.Core;
using GenSubtitle.App.ViewModels;

namespace GenSubtitle.App.Services;

/// <summary>
/// Manages UI state transitions following the state machine rules defined in the spec.
/// </summary>
public class ViewStateManager : ObservableObject
{
    private readonly ILogger _logger;
    private readonly ITaskQueueService _taskQueue;
    private ViewState _currentState = ViewState.Idle;

    public ViewState CurrentState
    {
        get => _currentState;
        private set => SetProperty(ref _currentState, value);
    }

    public ViewStateManager(ITaskQueueService taskQueue, ILogger logger)
    {
        _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Attempts to transition to a new state.
    /// Returns true if successful, false otherwise.
    /// </summary>
    public bool TransitionTo(ViewState newState, object? context = null)
    {
        if (!CanTransitionTo(newState))
        {
            _logger.LogWarning($"Invalid state transition: {CurrentState} → {newState}");
            return false;
        }

        try
        {
            var oldState = CurrentState;
            OnExitingState(oldState, newState);
            CurrentState = newState;
            OnEnteredState(newState, oldState);
            _logger.LogInformation($"State transition: {oldState} → {newState}");
            StateChanged?.Invoke(this, new StateChangedEventArgs(oldState, newState));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"State transition failed: {CurrentState} → {newState}");
            // Fallback to Idle state on error to ensure safety
            if (CurrentState != ViewState.Idle)
            {
                CurrentState = ViewState.Idle;
            }
            return false;
        }
    }

    /// <summary>
    /// Checks if transition to target state is valid.
    /// </summary>
    public bool CanTransitionTo(ViewState newState)
    {
        // State transition matrix as per spec:
        //         Idle  Processing  Editing
        // Idle      -        ✓         ✗
        // Processing ✓       -         ✓
        // Editing   ✓        ✓         -

        return (CurrentState, newState) switch
        {
            (ViewState.Idle, ViewState.Processing) => true,
            (ViewState.Idle, ViewState.Editing) => false,

            (ViewState.Processing, ViewState.Idle) => _taskQueue.Tasks.Count == 0,
            (ViewState.Processing, ViewState.Editing) => true,

            (ViewState.Editing, ViewState.Idle) => _taskQueue.Tasks.Count == 0,
            (ViewState.Editing, ViewState.Processing) => true,

            _ => false
        };
    }

    private void OnExitingState(ViewState oldState, ViewState newState)
    {
        // Cleanup when leaving a state
        switch (oldState)
        {
            case ViewState.Editing:
                _logger.LogInformation("Exiting Editing state, saving edit position");
                break;
        }
    }

    private void OnEnteredState(ViewState newState, ViewState oldState)
    {
        // Initialization when entering a state
        switch (newState)
        {
            case ViewState.Idle:
                _logger.LogInformation("Entered Idle state");
                break;
            case ViewState.Processing:
                _logger.LogInformation($"Entered Processing state with {_taskQueue.Tasks.Count} tasks");
                break;
            case ViewState.Editing:
                _logger.LogInformation("Entered Editing state");
                break;
        }
    }

    public event EventHandler<StateChangedEventArgs>? StateChanged;
}
