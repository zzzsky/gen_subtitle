using Xunit;
using GenSubtitle.App.ViewModels;
using GenSubtitle.App.Services;
using GenSubtitle.App.Core;
using Moq;
using System.Collections.ObjectModel;
using CoreTaskStatus = GenSubtitle.Core.Models.TaskStatus;

namespace GenSubtitle.Tests.Integration;

public class ProgressiveUIIntegrationTests
{
    [Fact]
    public void CompleteWorkflow_IdleToProcessingToIdle()
    {
        // Arrange
        var mockTaskQueue = new Mock<ITaskQueueService>();
        var logger = new ConsoleLogger();
        var manager = new ViewStateManager(mockTaskQueue.Object, logger);
        var observableCollection = new ObservableCollection<TaskItemViewModel>();

        mockTaskQueue.Setup(q => q.Tasks).Returns(observableCollection);

        // Act & Assert - Initial state is Idle
        Assert.Equal(ViewState.Idle, manager.CurrentState);

        // Simulate adding a task
        observableCollection.Add(null!); // Using null as placeholder

        // Can transition to Processing
        Assert.True(manager.CanTransitionTo(ViewState.Processing));
        Assert.True(manager.TransitionTo(ViewState.Processing));
        Assert.Equal(ViewState.Processing, manager.CurrentState);

        // Clear tasks
        observableCollection.Clear();

        // Can transition back to Idle
        Assert.True(manager.CanTransitionTo(ViewState.Idle));
        Assert.True(manager.TransitionTo(ViewState.Idle));
        Assert.Equal(ViewState.Idle, manager.CurrentState);
    }

    [Fact]
    public void StateTransitionMatrix_ValidTransitionsOnly()
    {
        var mockTaskQueue = new Mock<ITaskQueueService>();
        var logger = new ConsoleLogger();
        var manager = new ViewStateManager(mockTaskQueue.Object, logger);
        var tasks = new ObservableCollection<TaskItemViewModel>();
        mockTaskQueue.Setup(q => q.Tasks).Returns(tasks);

        // Idle -> Processing: Valid (when tasks exist)
        Assert.True(manager.CanTransitionTo(ViewState.Processing));

        // Idle -> Editing: Invalid
        Assert.False(manager.CanTransitionTo(ViewState.Editing));

        // Move to Processing
        tasks.Add(null!);
        manager.TransitionTo(ViewState.Processing);

        // Processing -> Idle: Invalid (tasks exist)
        Assert.False(manager.CanTransitionTo(ViewState.Idle));

        // Processing -> Editing: Valid
        Assert.True(manager.CanTransitionTo(ViewState.Editing));
    }
}
