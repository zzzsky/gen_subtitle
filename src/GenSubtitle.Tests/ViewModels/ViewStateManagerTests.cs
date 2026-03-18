using Xunit;
using GenSubtitle.App.Services;
using GenSubtitle.App.Core;
using GenSubtitle.App.ViewModels;
using Moq;
using System.Collections.ObjectModel;
using CoreTaskStatus = GenSubtitle.Core.Models.TaskStatus;

namespace GenSubtitle.Tests.ViewModels;

public class ViewStateManagerTests
{
    private readonly Mock<ITaskQueueService> _mockTaskQueue;
    private readonly Mock<ILogger> _mockLogger;

    public ViewStateManagerTests()
    {
        _mockTaskQueue = new Mock<ITaskQueueService>();
        _mockLogger = new Mock<ILogger>();
        _mockTaskQueue.Setup(q => q.Tasks).Returns(new ObservableCollection<TaskItemViewModel>());
    }

    [Fact]
    public void InitialState_IsIdle()
    {
        var manager = new ViewStateManager(_mockTaskQueue.Object, _mockLogger.Object);
        Assert.Equal(ViewState.Idle, manager.CurrentState);
    }

    [Fact]
    public void TransitionFromIdleToProcessing_WhenTasksExist_ReturnsTrue()
    {
        // Arrange
        var tasks = new ObservableCollection<TaskItemViewModel>();
        _mockTaskQueue.Setup(q => q.Tasks).Returns(tasks);
        var manager = new ViewStateManager(_mockTaskQueue.Object, _mockLogger.Object);
        // Add null as placeholder - collection count is what matters for state transitions
        tasks.Add(null!);

        // Act
        var result = manager.TransitionTo(ViewState.Processing);

        // Assert
        Assert.True(result);
        Assert.Equal(ViewState.Processing, manager.CurrentState);
    }

    [Fact]
    public void TransitionFromIdleToEditing_IsInvalid_ReturnsFalse()
    {
        // Arrange
        var manager = new ViewStateManager(_mockTaskQueue.Object, _mockLogger.Object);

        // Act
        var result = manager.TransitionTo(ViewState.Editing);

        // Assert
        Assert.False(result);
        Assert.Equal(ViewState.Idle, manager.CurrentState);
    }

    [Fact]
    public void TransitionFromProcessingToIdle_WhenTasksExist_ReturnsFalse()
    {
        // Arrange
        var tasks = new ObservableCollection<TaskItemViewModel>();
        _mockTaskQueue.Setup(q => q.Tasks).Returns(tasks);
        var manager = new ViewStateManager(_mockTaskQueue.Object, _mockLogger.Object);
        tasks.Add(null!);
        manager.TransitionTo(ViewState.Processing);

        // Act
        var result = manager.TransitionTo(ViewState.Idle);

        // Assert
        Assert.False(result);
        Assert.Equal(ViewState.Processing, manager.CurrentState);
    }

    [Fact]
    public void TransitionFromProcessingToIdle_WhenNoTasks_ReturnsTrue()
    {
        // Arrange
        var tasks = new ObservableCollection<TaskItemViewModel>();
        _mockTaskQueue.Setup(q => q.Tasks).Returns(tasks);
        var manager = new ViewStateManager(_mockTaskQueue.Object, _mockLogger.Object);
        tasks.Add(null!);
        manager.TransitionTo(ViewState.Processing);
        tasks.Clear();

        // Act
        var result = manager.TransitionTo(ViewState.Idle);

        // Assert
        Assert.True(result);
        Assert.Equal(ViewState.Idle, manager.CurrentState);
    }

    [Fact]
    public void TransitionFromProcessingToEditing_ReturnsTrue()
    {
        // Arrange
        var manager = new ViewStateManager(_mockTaskQueue.Object, _mockLogger.Object);
        manager.TransitionTo(ViewState.Processing);

        // Act
        var result = manager.TransitionTo(ViewState.Editing);

        // Assert
        Assert.True(result);
        Assert.Equal(ViewState.Editing, manager.CurrentState);
    }

    [Fact]
    public void TransitionFromEditingToProcessing_ReturnsTrue()
    {
        // Arrange
        var manager = new ViewStateManager(_mockTaskQueue.Object, _mockLogger.Object);
        manager.TransitionTo(ViewState.Processing);
        manager.TransitionTo(ViewState.Editing);

        // Act
        var result = manager.TransitionTo(ViewState.Processing);

        // Assert
        Assert.True(result);
        Assert.Equal(ViewState.Processing, manager.CurrentState);
    }

    [Fact]
    public void CanTransitionTo_ValidatesTransitionsCorrectly()
    {
        var tasks = new ObservableCollection<TaskItemViewModel>();
        _mockTaskQueue.Setup(q => q.Tasks).Returns(tasks);
        var manager = new ViewStateManager(_mockTaskQueue.Object, _mockLogger.Object);

        // Idle -> Processing: valid
        Assert.True(manager.CanTransitionTo(ViewState.Processing));

        // Idle -> Editing: invalid
        Assert.False(manager.CanTransitionTo(ViewState.Editing));

        // Processing -> Idle: depends on task count
        tasks.Add(null!);
        manager.TransitionTo(ViewState.Processing);
        Assert.False(manager.CanTransitionTo(ViewState.Idle));
    }
}
