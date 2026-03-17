# Progressive UI Implementation Plan - Phase 1

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement progressive disclosure UI framework with three states (Idle/Processing/Editing) that automatically transitions based on task status

**Architecture:** ViewStateManager controls state transitions following a state machine matrix. Three separate Views (Idle/Processing/Editing) with dedicated ViewModels. MainViewModel owns TaskQueueViewModel and passes ITaskQueueService to child ViewModels for loose coupling.

**Tech Stack:** WPF, Material Design in XAML, Custom MVVM (ObservableObject, RelayCommand), xUnit

---

## Scope

This plan covers **Phase 1** of the UI redesign:
- **Phase 1A:** Core framework (ViewStateManager, state enum, ITaskQueueService, IdleView)
- **Phase 1B:** Complete views (ProcessingView, EditingView, video player, subtitle editor, auto-transition)

**Out of scope (Phases 2-4):** Batch operations, search/filter, keyboard shortcuts, auto-save, and other enhancement features

---

## Prerequisites

Before starting implementation, create the required directory structure:

```bash
mkdir -p src/GenSubtitle.App/Core
mkdir -p src/GenSubtitle.App/Services
mkdir -p src/GenSubtitle.Tests/ViewModels
```

**Note:** The existing codebase does not have `Core/` or `Services/` directories in the App project. These must be created first.

---

## File Structure

### New Files to Create

```
src/GenSubtitle.App/
├── Core/
│   └── ViewState.cs                          # State enum
├── Services/
│   ├── ITaskQueueService.cs                  # Service interface
│   ├── ViewStateManager.cs                   # State machine
│   └── ConsoleLogger.cs                      # Simple logger
├── ViewModels/
│   ├── IdleViewModel.cs                      # Welcome page logic
│   ├── ProcessingViewModel.cs                # Progress page logic
│   └── EditingViewModel.cs                   # Editor page logic
├── Views/
│   ├── IdleView.xaml                         # Welcome page UI
│   ├── IdleView.xaml.cs
│   ├── ProcessingView.xaml                   # Progress page UI
│   ├── ProcessingView.xaml.cs
│   ├── EditingView.xaml                      # Editor page UI
│   └── EditingView.xaml.cs

src/GenSubtitle.Tests/
└── ViewModels/
    ├── ViewStateManagerTests.cs              # State transition tests
    └── IdleViewModelTests.cs                 # Idle view logic tests
```

### Files to Modify

```
src/GenSubtitle.App/
├── App.xaml                                  # Add resource dictionaries
├── App.xaml.cs                               # Register services if needed
├── ViewModels/
│   ├── ObservableObject.cs                   # Check RaisePropertyChanged signature
│   └── MainViewModel.cs                      # Add ViewStateManager, view switching
└── Views/
    └── MainWindow.xaml                       # Replace content with ContentControl
```
    └── IdleViewModelTests.cs                 # Idle view logic tests
```

### Files to Modify

```
src/GenSubtitle.App/
├── App.xaml                                  # Add resource dictionaries
├── App.xaml.cs                               # Register services if needed
├── ViewModels/
│   └── MainViewModel.cs                      # Add ViewStateManager, view switching
└── Views/
    └── MainWindow.xaml                       # Replace content with ContentControl
```

---

## Phase 1A: Core Framework (Tasks 1-12)

### Task 1: Create ViewState enum

**Files:**
- Create: `src/GenSubtitle.App/Core/ViewState.cs`

- [ ] **Step 1: Create ViewState enum**

```csharp
namespace GenSubtitle.App.Core;

/// <summary>
/// Represents the three possible UI states in the progressive interface
/// </summary>
public enum ViewState
{
    /// <summary>
    /// No tasks - shows welcome/guidance page
    /// </summary>
    Idle,

    /// <summary>
    /// Tasks are processing or in queue - shows progress overview
    /// </summary>
    Processing,

    /// <summary>
    /// User has selected a completed task for editing - shows video player and subtitle editor
    /// </summary>
    Editing
}
```

- [ ] **Step 2: Commit**

```bash
git add src/GenSubtitle.App/Core/ViewState.cs
git commit -m "feat(core): add ViewState enum for progressive UI"
```

---

### Task 2: Create ITaskQueueService interface

**Files:**
- Create: `src/GenSubtitle.App/Services/ITaskQueueService.cs`

- [ ] **Step 1: Create ITaskQueueService interface**

```csharp
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
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
```

- [ ] **Step 2: Commit**

```bash
git add src/GenSubtitle.App/Services/ITaskQueueService.cs
git commit -m "feat(services): add ITaskQueueService interface"
```

---

### Task 3: Create ViewStateManager with state machine logic

**Files:**
- Create: `src/GenSubtitle.App/Services/ViewStateManager.cs`
- Test: `src/GenSubtitle.Tests/ViewModels/ViewStateManagerTests.cs`

- [ ] **Step 1: Write failing tests for ViewStateManager**

```csharp
using Xunit;
using GenSubtitle.App.Services;
using GenSubtitle.App.Core;
using GenSubtitle.App.ViewModels;
using Moq;
using System.Collections.ObjectModel;

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
        var manager = new ViewStateManager(_mockTaskQueue.Object, _mockLogger.Object);
        _mockTaskQueue.Setup(q => q.Tasks.Count).Returns(1);

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
        var manager = new ViewStateManager(_mockTaskQueue.Object, _mockLogger.Object);
        manager.TransitionTo(ViewState.Processing);
        _mockTaskQueue.Setup(q => q.Tasks.Count).Returns(1);

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
        var manager = new ViewStateManager(_mockTaskQueue.Object, _mockLogger.Object);
        manager.TransitionTo(ViewState.Processing);
        _mockTaskQueue.Setup(q => q.Tasks.Count).Returns(0);

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
        var manager = new ViewStateManager(_mockTaskQueue.Object, _mockLogger.Object);

        // Idle -> Processing: valid
        Assert.True(manager.CanTransitionTo(ViewState.Processing));

        // Idle -> Editing: invalid
        Assert.False(manager.CanTransitionTo(ViewState.Editing));

        // Processing -> Idle: depends on task count
        _mockTaskQueue.Setup(q => q.Tasks.Count).Returns(1);
        manager.TransitionTo(ViewState.Processing);
        Assert.False(manager.CanTransitionTo(ViewState.Idle));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test src/GenSubtitle.Tests/GenSubtitle.Tests.csproj --filter "FullyQualifiedName~ViewStateManagerTests" -v n
```

Expected: Multiple FAIL errors with "ViewStateManager not defined"

- [ ] **Step 3: Create ViewStateManager implementation**

Create file: `src/GenSubtitle.App/Services/ViewStateManager.cs`

```csharp
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

/// <summary>
/// Event arguments for state changes
/// </summary>
public class StateChangedEventArgs : EventArgs
{
    public ViewState OldState { get; }
    public ViewState NewState { get; }

    public StateChangedEventArgs(ViewState oldState, ViewState newState)
    {
        OldState = oldState;
        NewState = newState;
    }
}

/// <summary>
/// Simple logger interface for dependency injection
/// </summary>
public interface ILogger
{
    void LogInformation(string message);
    void LogWarning(string message);
    void LogError(Exception ex, string message);
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test src/GenSubtitle.Tests/GenSubtitle.Tests.csproj --filter "FullyQualifiedName~ViewStateManagerTests" -v n
```

Expected: All PASS

- [ ] **Step 5: Commit**

```bash
git add src/GenSubtitle.App/Services/ViewStateManager.cs src/GenSubtitle.Tests/ViewModels/ViewStateManagerTests.cs
git commit -m "feat(services): implement ViewStateManager with state machine logic

- Add ViewStateManager with state transition matrix
- Implement CanTransitionTo() validation
- Add error handling with fallback to Idle state
- Add StateChanged event for notifications
- Include comprehensive unit tests"
```

---

### Task 4: Create simple console logger

**Files:**
- Create: `src/GenSubtitle.App/Services/ConsoleLogger.cs`

- [ ] **Step 1: Create ConsoleLogger implementation**

```csharp
using System;

namespace GenSubtitle.App.Services;

/// <summary>
/// Simple console logger implementation
/// </summary>
public class ConsoleLogger : ILogger
{
    public void LogInformation(string message)
    {
        Console.WriteLine($"[INFO] {DateTime.Now:HH:mm:ss} {message}");
    }

    public void LogWarning(string message)
    {
        Console.WriteLine($"[WARN] {DateTime.Now:HH:mm:ss} {message}");
    }

    public void LogError(Exception ex, string message)
    {
        Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} {message}");
        Console.WriteLine($"Exception: {ex}");
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/GenSubtitle.App/Services/ConsoleLogger.cs
git commit -m "feat(services): add ConsoleLogger implementation"
```

---

### Task 5: Create IdleViewModel

**Files:**
- Create: `src/GenSubtitle.App/ViewModels/IdleViewModel.cs`
- Test: `src/GenSubtitle.Tests/ViewModels/IdleViewModelTests.cs`

- [ ] **Step 1: Write failing tests for IdleViewModel**

```csharp
using Xunit;
using GenSubtitle.App.ViewModels;
using GenSubtitle.App.Services;
using Moq;

namespace GenSubtitle.Tests.ViewModels;

public class IdleViewModelTests
{
    private readonly Mock<ITaskQueueService> _mockTaskQueue;

    public IdleViewModelTests()
    {
        _mockTaskQueue = new Mock<ITaskQueueService>();
    }

    [Fact]
    public void ImportFilesCommand_WhenCalled_CallsEnqueueFiles()
    {
        // Arrange
        var viewModel = new IdleViewModel(_mockTaskQueue.Object);
        var files = new[] { "video1.mp4", "video2.mp4" };

        // Act
        viewModel.ImportFilesCommand.Execute(files);

        // Assert
        _mockTaskQueue.Verify(q => q.EnqueueFiles(files), Times.Once);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test src/GenSubtitle.Tests/GenSubtitle.Tests.csproj --filter "FullyQualifiedName~IdleViewModelTests" -v n
```

Expected: FAIL with "IdleViewModel not defined"

- [ ] **Step 3: Create IdleViewModel implementation**

```csharp
using System.Windows.Input;
using GenSubtitle.App.Services;

namespace GenSubtitle.App.ViewModels;

/// <summary>
/// ViewModel for the idle/welcome page
/// </summary>
public class IdleViewModel : ObservableObject
{
    private readonly ITaskQueueService _taskQueue;

    public IdleViewModel(ITaskQueueService taskQueue)
    {
        _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
        ImportFilesCommand = new RelayCommand(ImportFiles);
    }

    public ICommand ImportFilesCommand { get; }

    private void ImportFiles(object? parameter)
    {
        if (parameter is string[] files)
        {
            _taskQueue.EnqueueFiles(files);
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test src/GenSubtitle.Tests/GenSubtitle.Tests.csproj --filter "FullyQualifiedName~IdleViewModelTests" -v n
```

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/GenSubtitle.App/ViewModels/IdleViewModel.cs src/GenSubtitle.Tests/ViewModels/IdleViewModelTests.cs
git commit -m "feat(viewmodels): add IdleViewModel for welcome page"
```

---

### Task 6: Create IdleView XAML

**Files:**
- Create: `src/GenSubtitle.App/Views/IdleView.xaml`
- Create: `src/GenSubtitle.App/Views/IdleView.xaml.cs`

- [ ] **Step 1: Create IdleView.xaml**

```xml
<UserControl x:Class="GenSubtitle.App.Views.IdleView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
             xmlns:vm="clr-namespace:GenSubtitle.App.ViewModels"
             d:DataContext="{d:DesignInstance vm:IdleViewModel}">
    <Grid>
        <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center">
            <!-- Icon -->
            <materialDesign:PackIcon Kind="Film" Width="80" Height="80" Foreground="{DynamicResource PrimaryHueMidBrush}" HorizontalAlignment="Center" Margin="0,0,0,30"/>

            <!-- Title -->
            <TextBlock Text="欢迎使用 GenSubtitle" FontSize="32" FontWeight="SemiBold" HorizontalAlignment="Center" Margin="0,0,0,10"/>

            <!-- Subtitle -->
            <TextBlock Text="智能双语字幕生成工具" FontSize="16" Foreground="{DynamicResource MaterialDesignBodyLight}" HorizontalAlignment="Center" Margin="0,0,0,40"/>

            <!-- Import Button -->
            <Button Command="{Binding ImportFilesCommand}" CommandParameter="{Binding ImportedFiles}" Style="{StaticResource MaterialDesignRaisedButton}" Width="200" Height="50" HorizontalAlignment="Center" Margin="0,0,0,20">
                <StackPanel Orientation="Horizontal">
                    <materialDesign:PackIcon Kind="FolderOpen" Width="24" Height="24" Margin="0,0,10,0" VerticalAlignment="Center"/>
                    <TextBlock Text="导入视频文件" VerticalAlignment="Center"/>
                </StackPanel>
            </Button>

            <!-- Hint text -->
            <TextBlock Text="支持拖拽文件到窗口" Foreground="{DynamicResource MaterialDesignBodyLight}" HorizontalAlignment="Center" Margin="0,20,0,0" FontSize="13"/>

            <TextBlock Text="或点击上方按钮选择文件" Foreground="{DynamicResource MaterialDesignBodyLight}" HorizontalAlignment="Center" FontSize="13"/>
        </StackPanel>
    </Grid>
</UserControl>
```

- [ ] **Step 2: Create IdleView.xaml.cs code-behind**

```csharp
using System.Windows.Controls;

namespace GenSubtitle.App.Views;

public partial class IdleView : UserControl
{
    public IdleView()
    {
        InitializeComponent();
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add src/GenSubtitle.App/Views/IdleView.xaml src/GenSubtitle.App/Views/IdleView.xaml.cs
git commit -m "feat(views): add IdleView with welcome page UI"
```

---

### Task 7: Create ProcessingViewModel (basic version)

**Files:**
- Create: `src/GenSubtitle.App/ViewModels/ProcessingViewModel.cs`

- [ ] **Step 1: Create ProcessingViewModel implementation**

```csharp
using GenSubtitle.App.Services;

namespace GenSubtitle.App.ViewModels;

/// <summary>
/// ViewModel for the processing/progress page
/// </summary>
public class ProcessingViewModel : ObservableObject
{
    private readonly ITaskQueueService _taskQueue;

    public ProcessingViewModel(ITaskQueueService taskQueue)
    {
        _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
    }

    public ITaskQueueService TaskQueue => _taskQueue;
}
```

- [ ] **Step 2: Commit**

```bash
git add src/GenSubtitle.App/ViewModels/ProcessingViewModel.cs
git commit -m "feat(viewmodels): add ProcessingViewModel (basic)"
```

---

### Task 8: Create ProcessingView XAML (basic version)

**Files:**
- Create: `src/GenSubtitle.App/Views/ProcessingView.xaml`
- Create: `src/GenSubtitle.App/Views/ProcessingView.xaml.cs`

- [ ] **Step 1: Create ProcessingView.xaml**

```xml
<UserControl x:Class="GenSubtitle.App.Views.ProcessingView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- Statistics Card -->
        <materialDesign:Card Grid.Row="0" Margin="12,12,12,6" Padding="16">
            <TextBlock Text="📊 处理概览" FontSize="18" FontWeight="SemiBold"/>
        </materialDesign:Card>

        <!-- Placeholder for task list -->
        <Border Grid.Row="1" Background="{DynamicResource MaterialDesignPaper}" BorderBrush="{DynamicResource MaterialDesignDivider}" BorderThickness="1" Margin="12">
            <TextBlock Text="任务列表区域" HorizontalAlignment="Center" VerticalAlignment="Center" Foreground="{DynamicResource MaterialDesignBodyLight}"/>
        </Border>
    </Grid>
</UserControl>
```

- [ ] **Step 2: Create ProcessingView.xaml.cs code-behind**

```csharp
using System.Windows.Controls;

namespace GenSubtitle.App.Views;

public partial class ProcessingView : UserControl
{
    public ProcessingView()
    {
        InitializeComponent();
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add src/GenSubtitle.App/Views/ProcessingView.xaml src/GenSubtitle.App/Views/ProcessingView.xaml.cs
git commit -m "feat(views): add ProcessingView (basic layout)"
```

---

### Task 9: Create EditingViewModel (basic version)

**Files:**
- Create: `src/GenSubtitle.App/ViewModels/EditingViewModel.cs`

- [ ] **Step 1: Create EditingViewModel implementation**

```csharp
using GenSubtitle.App.Services;

namespace GenSubtitle.App.ViewModels;

/// <summary>
/// ViewModel for the editing page (video player + subtitle editor)
/// </summary>
public class EditingViewModel : ObservableObject
{
    private readonly ITaskQueueService _taskQueue;

    public EditingViewModel(ITaskQueueService taskQueue)
    {
        _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
    }

    public ITaskQueueService TaskQueue => _taskQueue;
}
```

- [ ] **Step 2: Commit**

```bash
git add src/GenSubtitle.App/ViewModels/EditingViewModel.cs
git commit -m "feat(viewmodels): add EditingViewModel (basic)"
```

---

### Task 10: Create EditingView XAML (basic version)

**Files:**
- Create: `src/GenSubtitle.App/Views/EditingView.xaml`
- Create: `src/GenSubtitle.App/Views/EditingView.xaml.cs`

- [ ] **Step 1: Create EditingView.xaml**

```xml
<UserControl x:Class="GenSubtitle.App.Views.EditingView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="280"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>

        <!-- Task list placeholder -->
        <Border Grid.Column="0" Background="{DynamicResource MaterialDesignPaper}" BorderBrush="{DynamicResource MaterialDesignDivider}" BorderThickness="0,0,1,0" Margin="0,0,0,0">
            <TextBlock Text="任务列表" HorizontalAlignment="Center" VerticalAlignment="Center" Foreground="{DynamicResource MaterialDesignBodyLight}"/>
        </Border>

        <!-- Editor placeholder -->
        <Grid Grid.Column="1">
            <TextBlock Text="视频预览 + 字幕编辑区域" HorizontalAlignment="Center" VerticalAlignment="Center" Foreground="{DynamicResource MaterialDesignBodyLight}"/>
        </Grid>
    </Grid>
</UserControl>
```

- [ ] **Step 2: Create EditingView.xaml.cs code-behind**

```csharp
using System.Windows.Controls;

namespace GenSubtitle.App.Views;

public partial class EditingView : UserControl
{
    public EditingView()
    {
        InitializeComponent();
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add src/GenSubtitle.App/Views/EditingView.xaml src/GenSubtitle.App/Views/EditingView.xaml.cs
git commit -m "feat(views): add EditingView (basic layout)"
```

---

### Task 11: Update MainViewModel to use ViewStateManager

**Files:**
- Modify: `src/GenSubtitle.App/ViewModels/MainViewModel.cs`

- [ ] **Step 1: Update MainViewModel with ViewStateManager integration**

First, read the current file to understand its structure, then add:

```csharp
// Add these using statements at the top
using GenSubtitle.App.Core;
using GenSubtitle.App.Services;
using System.Windows;

// Add these fields to the class
private readonly ViewStateManager _viewStateManager;
private readonly ITaskQueueService _taskQueueService;
private ObservableObject? _currentView;
private IdleViewModel? _idleViewModel;
private ProcessingViewModel? _processingViewModel;
private EditingViewModel? _editingViewModel;

public MainViewModel()
{
    _settings = _settingsService.Load();

    // Create task queue service wrapper
    _taskQueueService = new TaskQueueServiceAdapter(Queue);
    Queue.LoadCachedTasks();

    // Create ViewStateManager
    var logger = new ConsoleLogger();
    _viewStateManager = new ViewStateManager(_taskQueueService, logger);

    // Create child ViewModels
    _idleViewModel = new IdleViewModel(_taskQueueService);
    _processingViewModel = new ProcessingViewModel(_taskQueueService);
    _editingViewModel = new EditingViewModel(_taskQueueService);

    // Set initial view
    _currentView = _idleViewModel;

    // Subscribe to state changes
    _viewStateManager.StateChanged += OnStateChanged;

    // Subscribe to task queue changes for auto-transition
    Queue.Tasks.CollectionChanged += (s, e) => OnTasksChanged();
}

public ObservableObject CurrentView
{
    get => _currentView!;
    private set => SetProperty(ref _currentView, value);
}

public ViewState CurrentViewState => _viewStateManager.CurrentState;

private void OnStateChanged(object? sender, StateChangedEventArgs e)
{
    // Update current view based on new state
    CurrentView = e.NewState switch
    {
        ViewState.Idle => _idleViewModel,
        ViewState.Processing => _processingViewModel,
        ViewState.Editing => _editingViewModel,
        _ => _idleViewModel
    };
}

private void OnTasksChanged()
{
    // Auto-transition based on task queue state
    var currentState = _viewStateManager.CurrentState;

    if (Queue.Tasks.Count == 0 && currentState != ViewState.Idle)
    {
        // No tasks - go to idle
        _viewStateManager.TransitionTo(ViewState.Idle);
    }
    else if (Queue.Tasks.Count > 0 && currentState == ViewState.Idle)
    {
        // Tasks added - go to processing
        _viewStateManager.TransitionTo(ViewState.Processing);
    }
}

public void EnqueueFiles(string[] files)
{
    var first = Queue.EnqueueFiles(files);
    if (SelectedTask is null && first is not null)
    {
        SelectedTask = first;
    }

    // Trigger state transition check
    OnTasksChanged();
}

// Keep existing methods...
```

Also need to create TaskQueueServiceAdapter:

```csharp
/// <summary>
/// Adapter class that wraps TaskQueueViewModel to implement ITaskQueueService
/// </summary>
internal class TaskQueueServiceAdapter : ITaskQueueService
{
    private readonly TaskQueueViewModel _taskQueue;

    public TaskQueueServiceAdapter(TaskQueueViewModel taskQueue)
    {
        _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
    }

    public ObservableCollection<TaskItemViewModel> Tasks => _taskQueue.Tasks;

    public TaskItemViewModel? SelectedTask
    {
        get => _taskQueue.SelectedTask;
        set => _taskQueue.SelectedTask = value;
    }

    public TaskItemViewModel? EnqueueFiles(string[] files)
    {
        return _taskQueue.EnqueueFiles(files);
    }

    public Task ExportTaskAsync(TaskItemViewModel task, ExportOptions options, Action<double>? onProgress = null, CancellationToken cancellationToken = default)
    {
        return _taskQueue.ExportTaskAsync(task, options, onProgress, cancellationToken);
    }

    public void DeleteTask(TaskItemViewModel task, bool deleteFolder, bool force)
    {
        _taskQueue.DeleteTask(task, deleteFolder, force);
    }

    public void PauseTask(TaskItemViewModel task)
    {
        _taskQueue.PauseTask(task);
    }

    public void ResumeTask(TaskItemViewModel task)
    {
        _taskQueue.ResumeTask(task);
    }

    public void CancelTask(TaskItemViewModel task)
    {
        _taskQueue.CancelTask(task);
    }

    public void OpenFolder(TaskItemViewModel task)
    {
        _taskQueue.OpenFolder(task);
    }

    public bool CanExportSelected => _taskQueue.CanExportSelected;
}
```

- [ ] **Step 2: Commit**

```bash
git add src/GenSubtitle.App/ViewModels/MainViewModel.cs
git commit -m "feat(main): integrate ViewStateManager and view switching

- Add ViewStateManager to MainViewModel
- Create TaskQueueServiceAdapter for ITaskQueueService
- Initialize all three child ViewModels
- Implement OnStateChanged to switch views
- Implement OnTasksChanged for auto-transition
- Add CurrentView property for data binding"
```

---

### Task 12: Update MainWindow.xaml to use ContentControl

**Files:**
- Modify: `src/GenSubtitle.App/Views/MainWindow.xaml`

- [ ] **Step 1: Replace middle section with ContentControl**

Find the section starting with `<Grid Grid.Row="1" Margin="12">` (around line 99) and replace the entire Grid.Row="1" content with:

```xml
<ContentControl Grid.Row="1" Content="{Binding CurrentView}" Margin="12"/>
```

The full structure should now be:

```xml
<Grid>
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto" />
    <RowDefinition Height="*" />
    <RowDefinition Height="Auto" />
    <RowDefinition Height="180" />
  </Grid.RowDefinitions>

  <!-- Title Bar (keep as is) -->
  <Border Grid.Row="0" ...>...</Border>

  <!-- DYNAMIC CONTENT AREA -->
  <ContentControl Grid.Row="1" Content="{Binding CurrentView}" Margin="12"/>

  <!-- Status Bar (keep as is) -->
  <Border Grid.Row="2" ...>...</Border>

  <!-- Console (keep as is for now, will be removed in Phase 2) -->
  <materialDesign:Card Grid.Row="3" ...>...</materialDesign:Card>
</Grid>
```

- [ ] **Step 2: Add DataTemplate for each view in App.xaml**

Modify `src/GenSubtitle.App/App.xaml`:

```xml
<Application.Resources>
  <!-- Existing resources... -->

  <!-- DataTemplates for view switching -->
  <DataTemplate DataType="{x:Type vm:IdleViewModel}">
    <v:IdleView/>
  </DataTemplate>

  <DataTemplate DataType="{x:Type vm:ProcessingViewModel}">
    <v:ProcessingView/>
  </DataTemplate>

  <DataTemplate DataType="{x:Type vm:EditingViewModel}">
    <v:EditingView/>
  </DataTemplate>
</Application.Resources>
```

Add namespace declarations at the top of App.xaml:

```xml
xmlns:vm="clr-namespace:GenSubtitle.App.ViewModels"
xmlns:v="clr-namespace:GenSubtitle.App.Views"
```

- [ ] **Step 3: Commit**

```bash
git add src/GenSubtitle.App/Views/MainWindow.xaml src/GenSubtitle.App/App.xaml
git commit -m "feat(ui): replace MainWindow middle section with ContentControl

- Add ContentControl for dynamic view switching
- Add DataTemplates for Idle/Processing/Editing views
- Keep title bar, status bar, and console unchanged
- Views now automatically switch based on CurrentView binding"
```

---

## Phase 1B: Complete Views (Tasks 13-25)

### Task 13: Implement task list display in ProcessingView

**Files:**
- Modify: `src/GenSubtitle.App/Views/ProcessingView.xaml`
- Modify: `src/GenSubtitle.App/ViewModels/ProcessingViewModel.cs`

- [ ] **Step 1: Update ProcessingViewModel to expose Tasks**

```csharp
public ObservableCollection<TaskItemViewModel> Tasks => _taskQueue.Tasks;

public TaskItemViewModel? SelectedTask
{
    get => _taskQueue.SelectedTask;
    set
    {
        if (_taskQueue.SelectedTask != value)
        {
            _taskQueue.SelectedTask = value;
            RaisePropertyChanged(nameof(SelectedTask));
        }
    }
}
```

- [ ] **Step 2: Update ProcessingView.xaml to show task list**

Replace the placeholder border with:

```xml
<UserControl x:Class="GenSubtitle.App.Views.ProcessingView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- Statistics Card -->
        <materialDesign:Card Grid.Row="0" Margin="12,12,12,6" Padding="16">
            <StackPanel>
                <TextBlock Text="📊 处理概览" FontSize="18" FontWeight="SemiBold" Margin="0,0,0,8"/>
                <TextBlock Text="{Binding TasksSummary}" FontSize="14"/>
            </StackPanel>
        </materialDesign:Card>

        <!-- Task List -->
        <ScrollViewer Grid.Row="1" VerticalScrollBarVisibility="Auto">
            <ItemsControl ItemsSource="{Binding Tasks}">
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <materialDesign:Card Margin="12,6,12,6" Padding="12">
                            <Grid>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="Auto"/>
                                    <RowDefinition Height="Auto"/>
                                </Grid.RowDefinitions>

                                <!-- File Name -->
                                <TextBlock Grid.Row="0" Text="{Binding FileName}" FontWeight="SemiBold" FontSize="14"/>

                                <!-- Status and Progress -->
                                <StackPanel Grid.Row="1" Margin="0,8,0,0">
                                    <TextBlock Text="{Binding StatusText}" FontSize="12" Foreground="{DynamicResource MaterialDesignBodyLight}"/>
                                    <ProgressBar Value="{Binding Progress}" Maximum="100" Height="4" Margin="0,4,0,0"/>
                                </StackPanel>
                            </Grid>
                        </materialDesign:Card>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </ScrollViewer>
    </Grid>
</UserControl>
```

- [ ] **Step 3: Add TasksSummary property to ProcessingViewModel**

```csharp
public string TasksSummary
{
    get
    {
        var total = Tasks.Count;
        var processing = Tasks.Count(t => t.Status == CoreTaskStatus.Transcribing || t.Status == CoreTaskStatus.Translating);
        var completed = Tasks.Count(t => t.Status == CoreTaskStatus.Completed);
        var failed = Tasks.Count(t => t.Status == CoreTaskStatus.Failed);

        return $"运行中: {processing}/{total} | 已完成: {completed} | 失败: {failed}";
    }
}
```

You'll need to add `using CoreTaskStatus = GenSubtitle.Core.Models.TaskStatus;` at the top.

- [ ] **Step 4: Commit**

```bash
git add src/GenSubtitle.App/Views/ProcessingView.xaml src/GenSubtitle.App/ViewModels/ProcessingViewModel.cs
git commit -m "feat(processing): add task list display with statistics

- Show task list with cards
- Display status and progress for each task
- Add summary statistics at top
- Tasks update in real-time via ObservableCollection"
```

---

### Task 14: Implement video player in EditingView

**Files:**
- Modify: `src/GenSubtitle.App/Views/EditingView.xaml`
- Modify: `src/GenSubtitle.App/Views/EditingView.xaml.cs`

- [ ] **Step 1: Update EditingView.xaml with video player**

```xml
<UserControl x:Class="GenSubtitle.App.Views.EditingView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="280"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>

        <!-- Task List -->
        <Border Grid.Column="0" Background="{DynamicResource MaterialDesignPaper}" BorderBrush="{DynamicResource MaterialDesignDivider}" BorderThickness="0,0,1,0">
            <ScrollViewer VerticalScrollBarVisibility="Auto">
                <ItemsControl ItemsSource="{Binding TaskQueue.Tasks}">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate>
                            <Border Padding="12" Background="{DynamicResource MaterialDesignCardBackground}" Margin="8,4,8,4" BorderBrush="{DynamicResource MaterialDesignDivider}" BorderThickness="1">
                                <StackPanel>
                                    <TextBlock Text="{Binding FileName}" FontWeight="SemiBold" TextTrimming="CharacterEllipsis"/>
                                    <TextBlock Text="{Binding StatusText}" FontSize="11" Foreground="{DynamicResource MaterialDesignBodyLight}" Margin="0,4,0,0"/>
                                </StackPanel>
                            </Border>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </ScrollViewer>
        </Border>

        <!-- Editor Area -->
        <Grid Grid.Column="1">
            <Grid.RowDefinitions>
                <RowDefinition Height="*"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>

            <!-- Video Player -->
            <materialDesign:Card Grid.Row="0" Margin="8">
                <Border Background="#0E0E0E" CornerRadius="4" Margin="8">
                    <MediaElement x:Name="Player" LoadedBehavior="Manual" UnloadedBehavior="Manual" ScrubbingEnabled="True" Stretch="Uniform"/>
                </Border>
            </materialDesign:Card>

            <!-- Player Controls -->
            <StackPanel Grid.Row="1" Orientation="Horizontal" HorizontalAlignment="Center" Margin="0,8,0,8">
                <Button Content="播放" Style="{StaticResource MaterialDesignRaisedButton}" Width="80" Margin="0,0,8,0" Click="OnPlay"/>
                <Button Content="暂停" Style="{StaticResource MaterialDesignRaisedButton}" Width="80" Margin="0,0,8,0" Click="OnPause"/>
                <Button Content="对齐开始" Style="{StaticResource MaterialDesignOutlinedButton}" Width="90" Margin="0,0,8,0" Click="OnAlignStart"/>
                <Button Content="对齐结束" Style="{StaticResource MaterialDesignOutlinedButton}" Width="90" Click="OnAlignEnd"/>
            </StackPanel>

            <!-- Timeline Slider -->
            <StackPanel Grid.Row="1" Orientation="Vertical" Margin="150,8,150,0">
                <Slider x:Name="TimelineSlider" Minimum="0" Maximum="1" ValueChanged="OnTimelineChanged"/>
                <TextBlock x:Name="TimeDisplay" Text="00:00 / 00:00" HorizontalAlignment="Center" FontSize="12" Foreground="{DynamicResource MaterialDesignBodyLight}" Margin="0,4,0,0"/>
            </StackPanel>

            <!-- Subtitle Editor (placeholder for now) -->
            <materialDesign:Card Grid.Row="2" Margin="8,8,8,8">
                <ScrollViewer VerticalScrollBarVisibility="Auto">
                    <TextBlock Text="字幕编辑表格区域" HorizontalAlignment="Center" VerticalAlignment="Center" Foreground="{DynamicResource MaterialDesignBodyLight}"/>
                </ScrollViewer>
            </materialDesign:Card>
        </Grid>
    </Grid>
</UserControl>
```

- [ ] **Step 2: Update EditingView.xaml.cs with player controls**

```csharp
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace GenSubtitle.App.Views;

public partial class EditingView : UserControl
{
    private readonly DispatcherTimer _timer;
    private bool _isSeeking = false;

    public EditingView()
    {
        InitializeComponent();

        // Timer for updating timeline
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (Player.NaturalDuration.HasTimeSpan && Player.Position != null && !_isSeeking)
        {
            TimelineSlider.Maximum = Player.NaturalDuration.TimeSpan.TotalSeconds;
            TimelineSlider.Value = Player.Position.TotalSeconds;
            UpdateTimeDisplay();
        }
    }

    private void UpdateTimeDisplay()
    {
        var current = Player.Position.HasValue ? Player.Position.Value : TimeSpan.Zero;
        var total = Player.NaturalDuration.HasTimeSpan ? Player.NaturalDuration.TimeSpan : TimeSpan.Zero;
        TimeDisplay.Text = $"{current:mm\\:ss} / {total:mm\\:ss}";
    }

    private void OnPlay(object sender, RoutedEventArgs e)
    {
        Player.Play();
    }

    private void OnPause(object sender, RoutedEventArgs e)
    {
        Player.Pause();
    }

    private void OnTimelineChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isSeeking && Player.NaturalDuration.HasTimeSpan)
        {
            Player.Position = TimeSpan.FromSeconds(e.NewValue);
            UpdateTimeDisplay();
        }
    }

    private void OnAlignStart(object sender, RoutedEventArgs e)
    {
        // TODO: Implement in next task
    }

    private void OnAlignEnd(object sender, RoutedEventArgs e)
    {
        // TODO: Implement in next task
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add src/GenSubtitle.App/Views/EditingView.xaml src/GenSubtitle.App/Views/EditingView.xaml.cs
git commit -m "feat(editing): add video player with controls

- Add MediaElement for video playback
- Implement play/pause controls
- Add timeline slider with time display
- Show task list in left sidebar
- Placeholder for subtitle editor"
```

---

### Task 15: Implement subtitle editor DataGrid

**Files:**
- Modify: `src/GenSubtitle.App/Views/EditingView.xaml`

- [ ] **Step 1: Replace subtitle editor placeholder with DataGrid**

```xml
<!-- Subtitle Editor -->
<materialDesign:Card Grid.Row="2" Margin="8,8,8,8">
    <DataGrid ItemsSource="{Binding TaskQueue.SelectedTask.Segments}"
              AutoGenerateColumns="False"
              CanUserAddRows="False"
              CanUserDeleteRows="False"
              SelectionMode="Single"
              HeadersVisibility="Column"
              GridLinesVisibility="Horizontal"
              materialDesign:DataGridAssist.CellPadding="8">
        <DataGrid.Columns>
            <DataGridTextColumn Header="#" Binding="{Binding Id}" Width="40" IsReadOnly="True"/>
            <DataGridTextColumn Header="开始" Binding="{Binding Start, StringFormat=hh\\:mm\\:ss\\.fff}" Width="90"/>
            <DataGridTextColumn Header="结束" Binding="{Binding End, StringFormat=hh\\:mm\\:ss\\.fff}" Width="90"/>
            <DataGridTextColumn Header="原文" Binding="{Binding SourceText}" Width="*"/>
            <DataGridTextColumn Header="中文" Binding="{Binding ZhText}" Width="*"/>
        </DataGrid.Columns>
    </DataGrid>
</materialDesign:Card>
```

- [ ] **Step 2: Commit**

```bash
git add src/GenSubtitle.App/Views/EditingView.xaml
git commit -m "feat(editing): add subtitle editor DataGrid

- Display subtitle segments in editable table
- Show ID, start time, end time, source text, Chinese text
- Support inline editing
- Read-only ID column"
```

---

### Task 16: Implement auto-state-transition on task selection

**Files:**
- Modify: `src/GenSubtitle.App/ViewModels/MainViewModel.cs`

- [ ] **Step 1: Add task selection handler**

```csharp
private void OnTasksChanged()
{
    // Existing logic...
}

// Add new method
private void OnTaskSelectionChanged()
{
    var selectedTask = _taskQueue.SelectedTask;
    var currentState = _viewStateManager.CurrentState;

    if (selectedTask == null)
    {
        // No task selected
        if (_taskQueue.Tasks.Count == 0)
        {
            _viewStateManager.TransitionTo(ViewState.Idle);
        }
        else if (currentState == ViewState.Editing)
        {
            _viewStateManager.TransitionTo(ViewState.Processing);
        }
    }
    else if (selectedTask.Status == CoreTaskStatus.Completed && currentState != ViewState.Editing)
    {
        // Completed task selected - switch to editing
        _viewStateManager.TransitionTo(ViewState.Editing);
    }
    else if (selectedTask.Status != CoreTaskStatus.Completed && currentState == ViewState.Editing)
    {
        // Non-completed task selected while in editing - switch to processing
        _viewStateManager.TransitionTo(ViewState.Processing);
    }
}
```

- [ ] **Step 2: Subscribe to selection changes in MainViewModel constructor**

```csharp
// Subscribe to task queue changes
Queue.Tasks.CollectionChanged += (s, e) => OnTasksChanged();
Queue.PropertyChanged += (s, e) =>
{
    if (e.PropertyName == nameof(TaskQueueViewModel.SelectedTask))
    {
        OnTaskSelectionChanged();
    }
};
```

- [ ] **Step 3: Commit**

```bash
git add src/GenSubtitle.App/ViewModels/MainViewModel.cs
git commit -m "feat(main): add auto-state-transition on task selection

- Transition to Editing when completed task selected
- Transition to Processing when non-completed task selected
- Return to Processing when no task selected
- Handle edge cases correctly"
```

---

### Task 17: Add navigation buttons to ProcessingView

**Files:**
- Modify: `src/GenSubtitle.App/Views/ProcessingView.xaml`
- Modify: `src/GenSubtitle.App/ViewModels/ProcessingViewModel.cs`

- [ ] **Step 1: Add navigation command to ProcessingViewModel**

```csharp
public ICommand ReturnToIdleCommand { get; }

public ProcessingViewModel(ITaskQueueService taskQueue) : base(taskQueue)
{
    ReturnToIdleCommand = new RelayCommand(ReturnToIdle, CanReturnToIdle);
}

private bool CanReturnToIdle()
{
    return _taskQueue.Tasks.Count == 0;
}

private void ReturnToIdle()
{
    // Clear all tasks
    var tasks = _taskQueue.Tasks.ToList();
    foreach (var task in tasks)
    {
        _taskQueue.DeleteTask(task, false, true);
    }
}
```

- [ ] **Step 2: Add button to ProcessingView header**

Update the statistics card:

```xml
<materialDesign:Card Grid.Row="0" Margin="12,12,12,6" Padding="16">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
        </Grid.ColumnDefinitions>

        <StackPanel Grid.Column="0">
            <TextBlock Text="📊 处理概览" FontSize="18" FontWeight="SemiBold" Margin="0,0,0,8"/>
            <TextBlock Text="{Binding TasksSummary}" FontSize="14"/>
        </StackPanel>

        <Button Grid.Column="1"
                Content="返回引导页"
                Command="{Binding ReturnToIdleCommand}"
                Style="{StaticResource MaterialDesignOutlinedButton}"
                Margin="12,0,0,0"/>
    </Grid>
</materialDesign:Card>
```

- [ ] **Step 3: Commit**

```bash
git add src/GenSubtitle.App/Views/ProcessingView.xaml src/GenSubtitle.App/ViewModels/ProcessingViewModel.cs
git commit -m "feat(processing): add return to idle button

- Add ReturnToIdle command
- Clear all tasks when returning to idle
- Button only enabled when no tasks exist"
```

---

### Task 18: Add navigation buttons to EditingView

**Files:**
- Modify: `src/GenSubtitle.App/Views/EditingView.xaml`
- Modify: `src/GenSubtitle.App/ViewModels/EditingViewModel.cs`

- [ ] **Step 1: Add navigation command to EditingViewModel**

```csharp
public ICommand ReturnToProcessingCommand { get; }

public EditingViewModel(ITaskQueueService taskQueue) : base(taskQueue)
{
    ReturnToProcessingCommand = new RelayCommand(ReturnToProcessing);
}

private void ReturnToProcessing()
{
    // Deselect current task to trigger state change
    _taskQueue.SelectedTask = null;
}
```

- [ ] **Step 2: Add button to EditingView header**

Add this before the Grid.ColumnDefinitions:

```xml
<Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
</Grid.RowDefinitions>

<!-- Header with navigation -->
<materialDesign:Card Grid.Row="0" Margin="8,8,8,8" Padding="12,8,12,8">
    <Grid>
        <TextBlock Text="✏️ 字幕编辑" FontSize="18" FontWeight="SemiBold" VerticalAlignment="Center"/>
        <Button Content="返回进度页"
                Command="{Binding ReturnToProcessingCommand}"
                Style="{StaticResource MaterialDesignOutlinedButton}"
                HorizontalAlignment="Right"
                Padding="16,6"/>
    </Grid>
</materialDesign:Card>
```

Then change the main Grid to use Grid.Row="1":

```xml
<!-- Main content -->
<Grid Grid.Row="1">
    <Grid.ColumnDefinitions>
        ...
```

- [ ] **Step 3: Commit**

```bash
git add src/GenSubtitle.App/Views/EditingView.xaml src/GenSubtitle.App/ViewModels/EditingViewModel.cs
git commit -m "feat(editing): add return to processing button

- Add ReturnToProcessing command
- Deselect task to trigger state transition
- Add header card with navigation"
```

---

### Task 19: Implement time alignment functionality

**Files:**
- Modify: `src/GenSubtitle.App/Views/EditingView.xaml.cs`

- [ ] **Step 1: Implement alignment handlers**

```csharp
private void OnAlignStart(object sender, RoutedEventArgs e)
{
    if (DataContext is EditingViewModel vm && vm.TaskQueue.SelectedTask is not null)
    {
        var position = Player.Position;
        vm.AlignSelectedStart(position);
    }
}

private void OnAlignEnd(object sender, RoutedEventArgs e)
{
    if (DataContext is EditingViewModel vm && vm.TaskQueue.SelectedTask is not null)
    {
        var position = Player.Position;
        vm.AlignSelectedEnd(position);
    }
}
```

- [ ] **Step 2: Add alignment methods to EditingViewModel**

```csharp
public void AlignSelectedStart(TimeSpan position)
{
    if (_taskQueue.SelectedTask?.SelectedSegment is not null)
    {
        _taskQueue.SelectedTask.SelectedSegment.Start = position;
    }
}

public void AlignSelectedEnd(TimeSpan position)
{
    if (_taskQueue.SelectedTask?.SelectedSegment is not null)
    {
        _taskQueue.SelectedTask.SelectedSegment.End = position;
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add src/GenSubtitle.App/Views/EditingView.xaml.cs src/GenSubtitle.App/ViewModels/EditingViewModel.cs
git commit -m "feat(editing): implement time alignment functionality

- Align selected segment start to current playback position
- Align selected segment end to current playback position
- Use existing SelectedSegment from TaskItemViewModel"
```

---

### Task 20: Add video file loading to player

**Files:**
- Modify: `src/GenSubtitle.App/Views/EditingView.xaml.cs`
- Modify: `src/GenSubtitle.App/ViewModels/EditingViewModel.cs`

- [ ] **Step 1: Add video path property to EditingViewModel**

```csharp
private string? _currentVideoPath;

public string? CurrentVideoPath
{
    get => _currentVideoPath;
    private set => SetProperty(ref _currentVideoPath, value);
}

private void OnSelectedTaskChanged()
{
    if (_taskQueue.SelectedTask is not null)
    {
        CurrentVideoPath = _taskQueue.SelectedTask.FilePath;

        // Load video in player
        LoadVideoRequested?.Invoke(this, CurrentVideoPath);
    }
}

public event EventHandler<string>? LoadVideoRequested;
```

Call this method when the view is loaded or task changes:

```csharp
public EditingViewModel(ITaskQueueService taskQueue) : base(taskQueue)
{
    ReturnToProcessingCommand = new RelayCommand(ReturnToProcessing);

    // Subscribe to task changes
    _taskQueue.Tasks.CollectionChanged += (s, e) => OnTaskCollectionChanged();
    // Note: You'll need to add INotifyPropertyChanged support for SelectedTask
}

private void OnTaskCollectionChanged()
{
    if (_taskQueue.SelectedTask is not null)
    {
        CurrentVideoPath = _taskQueue.SelectedTask.FilePath;
        LoadVideoRequested?.Invoke(this, CurrentVideoPath);
    }
}
```

- [ ] **Step 2: Subscribe to LoadVideoRequested in EditingView**

```csharp
public EditingView()
{
    InitializeComponent();

    _timer = new DispatcherTimer
    {
        Interval = TimeSpan.FromMilliseconds(100)
    };
    _timer.Tick += OnTimerTick;
    _timer.Start();

    // Subscribe to video load requests
    this.Loaded += (s, e) =>
    {
        if (DataContext is EditingViewModel vm)
        {
            vm.LoadVideoRequested += OnLoadVideoRequested;
        }
    };

    this.Unloaded += (s, e) =>
    {
        if (DataContext is EditingViewModel vm)
        {
            vm.LoadVideoRequested -= OnLoadVideoRequested;
        }
    };

    this.DataContextChanged += (s, e) =>
    {
        if (e.OldValue is EditingViewModel oldVm)
        {
            oldVm.LoadVideoRequested -= OnLoadVideoRequested;
        }
        if (e.NewValue is EditingViewModel newVm)
        {
            newVm.LoadVideoRequested += OnLoadVideoRequested;
        }
    };
}

private void OnLoadVideoRequested(object? sender, string videoPath)
{
    if (File.Exists(videoPath))
    {
        Player.Source = new Uri(videoPath);
        Player.Position = TimeSpan.Zero;
    }
}
```

Add `using System.IO;` at the top.

- [ ] **Step 3: Commit**

```bash
git add src/GenSubtitle.App/Views/EditingView.xaml.cs src/GenSubtitle.App/ViewModels/EditingViewModel.cs
git commit -m "feat(editing): add video file loading

- Load video file when task is selected
- Update player Source property
- Reset position to zero
- Handle file not found gracefully"
```

---

### Task 21: Add task selection highlighting

**Files:**
- Modify: `src/GenSubtitle.App/Views/ProcessingView.xaml`
- Modify: `src/GenSubtitle.App/Views/EditingView.xaml`

- [ ] **Step 1: Update ProcessingView task list with selection**

Replace the ItemsControl with ListBox:

```xml
<!-- Task List -->
<ListBox Grid.Row="1"
         ItemsSource="{Binding Tasks}"
         SelectedItem="{Binding SelectedTask}"
         SelectionMode="Single"
         BorderThickness="0"
         Background="Transparent"
         Padding="6">
    <ListBox.ItemTemplate>
        <DataTemplate>
            <materialDesign:Card Margin="6" Padding="12">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>

                    <!-- File Name -->
                    <TextBlock Grid.Row="0" Text="{Binding FileName}" FontWeight="SemiBold" FontSize="14"/>

                    <!-- Status and Progress -->
                    <StackPanel Grid.Row="1" Margin="0,8,0,0">
                        <TextBlock Text="{Binding StatusText}" FontSize="12" Foreground="{DynamicResource MaterialDesignBodyLight}"/>
                        <ProgressBar Value="{Binding Progress}" Maximum="100" Height="4" Margin="0,4,0,0"/>
                    </StackPanel>
                </Grid>
            </materialDesign:Card>
        </DataTemplate>
    </ListBox.ItemTemplate>
    <ListBox.ItemContainerStyle>
        <Style TargetType="ListBoxItem" BasedOn="{StaticResource MaterialDesignListBoxItem}">
            <Setter Property="Padding" Value="0"/>
            <Setter Property="Margin" Value="0"/>
            <Setter Property="Background" Value="Transparent"/>
        </Style>
    </ListBox.ItemContainerStyle>
</ListBox>
```

- [ ] **Step 2: Update EditingView task list with selection**

Replace the ItemsControl with ListBox:

```xml
<!-- Task List -->
<ListBox Grid.Column="0"
         ItemsSource="{Binding TaskQueue.Tasks}"
         SelectedItem="{Binding TaskQueue.SelectedTask}"
         SelectionMode="Single"
         BorderThickness="0"
         Background="Transparent">
    <ListBox.ItemTemplate>
        <DataTemplate>
            <Border Padding="12"
                    Background="{DynamicResource MaterialDesignCardBackground}"
                    Margin="8,4,8,4"
                    BorderBrush="{DynamicResource MaterialDesignDivider}"
                    BorderThickness="1">
                <StackPanel>
                    <TextBlock Text="{Binding FileName}" FontWeight="SemiBold" TextTrimming="CharacterEllipsis"/>
                    <TextBlock Text="{Binding StatusText}" FontSize="11" Foreground="{DynamicResource MaterialDesignBodyLight}" Margin="0,4,0,0"/>
                </StackPanel>
            </Border>
        </DataTemplate>
    </ListBox.ItemTemplate>
    <ListBox.ItemContainerStyle>
        <Style TargetType="ListBoxItem" BasedOn="{StaticResource MaterialDesignListBoxItem}">
            <Setter Property="Padding" Value="0"/>
            <Setter Property="Margin" Value="0"/>
        </Style>
    </ListBox.ItemContainerStyle>
</ListBox>
```

- [ ] **Step 3: Commit**

```bash
git add src/GenSubtitle.App/Views/ProcessingView.xaml src/GenSubtitle.App/Views/EditingView.xaml
git commit -m "feat(views): add task selection with highlighting

- Replace ItemsControl with ListBox for selection support
- SelectedItem binding to highlight selected task
- Remove padding from ListBoxItem for better appearance"
```

---

### Task 22: Implement error handling for state transitions

**Files:**
- Modify: `src/GenSubtitle.App/ViewModels/MainViewModel.cs`

- [ ] **Step 1: Add error handling wrappers**

```csharp
private void OnStateChanged(object? sender, StateChangedEventArgs e)
{
    try
    {
        // Update current view based on new state
        CurrentView = e.NewState switch
        {
            ViewState.Idle => _idleViewModel,
            ViewState.Processing => _processingViewModel,
            ViewState.Editing => _editingViewModel,
            _ => _idleViewModel
        };
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, $"Error switching to view: {e.NewState}");
        // Fallback to idle view
        CurrentView = _idleViewModel;
    }
}

private void OnTasksChanged()
{
    try
    {
        // Existing logic...
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "Error in OnTasksChanged");
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/GenSubtitle.App/ViewModels/MainViewModel.cs
git commit -m "feat(main): add error handling for state transitions

- Wrap state change logic in try-catch
- Fallback to idle view on error
- Log errors for debugging"
```

---

### Task 23: Add drag-and-drop file import to IdleView

**Files:**
- Modify: `src/GenSubtitle.App/Views/IdleView.xaml`
- Modify: `src/GenSubtitle.App/Views/IdleView.xaml.cs`

- [ ] **Step 1: Enable drag-and-drop in IdleView.xaml**

Update the UserControl tag:

```xml
<UserControl x:Class="GenSubtitle.App.Views.IdleView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
             xmlns:vm="clr-namespace:GenSubtitle.App.ViewModels"
             d:DataContext="{d:DesignInstance vm:IdleViewModel}"
             AllowDrop="True"
             DragEnter="OnDragEnter"
             DragLeave="OnDragLeave"
             Drop="OnDrop">
```

- [ ] **Step 2: Add drag-and-drop event handlers**

```csharp
using System.IO;
using System.Windows;

public partial class IdleView : UserControl
{
    public IdleView()
    {
        InitializeComponent();
    }

    private void OnDragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void OnDragLeave(object sender, DragEventArgs e)
    {
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop) && DataContext is ViewModel)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files?.Length > 0)
            {
                if (DataContext is IdleViewModel vm)
                {
                    vm.ImportFilesCommand.Execute(files);
                }
            }
        }
        e.Handled = true;
    }
}
```

- [ ] **Step 3: Add visual feedback for drag-over**

Add to the UserControl resources or modify the background:

```xml
<UserControl.Background>
    <SolidColorBrush Color="Transparent"/>
</UserControl.Background>
```

- [ ] **Step 4: Commit**

```bash
git add src/GenSubtitle.App/Views/IdleView.xaml src/GenSubtitle.App/Views/IdleView.xaml.cs
git commit -m "feat(idle): add drag-and-drop file import

- Allow dragging video files onto idle view
- Handle FileDrop data format
- Execute ImportFilesCommand with dropped files
- Add visual feedback during drag"
```

---

### Task 24: Integration test - complete user flow

**Files:**
- Create: `src/GenSubtitle.Tests/Integration/ProgressiveUIIntegrationTests.cs`

- [ ] **Step 1: Write integration test**

```csharp
using Xunit;
using GenSubtitle.App.ViewModels;
using GenSubtitle.App.Services;
using GenSubtitle.App.Core;
using Moq;

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
        var observableCollection = new System.Collections.ObjectModel.ObservableCollection<TaskItemViewModel>();

        mockTaskQueue.Setup(q => q.Tasks).Returns(observableCollection);

        // Act & Assert - Initial state is Idle
        Assert.Equal(ViewState.Idle, manager.CurrentState);

        // Simulate adding a task
        var mockTask = new Mock<TaskItemViewModel>();
        mockTask.Setup(t => t.Status).Returns(CoreTaskStatus.Queued);
        observableCollection.Add(mockTask.Object);

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
        var tasks = new System.Collections.ObjectModel.ObservableCollection<TaskItemViewModel>();
        mockTaskQueue.Setup(q => q.Tasks).Returns(tasks);

        // Idle -> Processing: Valid (when tasks exist)
        Assert.True(manager.CanTransitionTo(ViewState.Processing));

        // Idle -> Editing: Invalid
        Assert.False(manager.CanTransitionTo(ViewState.Editing));

        // Move to Processing
        manager.TransitionTo(ViewState.Processing);

        // Processing -> Idle: Invalid (tasks exist)
        Assert.False(manager.CanTransitionTo(ViewState.Idle));

        // Processing -> Editing: Valid
        Assert.True(manager.CanTransitionTo(ViewState.Editing));
    }
}
```

- [ ] **Step 2: Run integration tests**

```bash
dotnet test src/GenSubtitle.Tests/GenSubtitle.Tests.csproj --filter "FullyQualifiedName~ProgressiveUIIntegrationTests" -v n
```

Expected: All PASS

- [ ] **Step 3: Commit**

```bash
git add src/GenSubtitle.Tests/Integration/ProgressiveUIIntegrationTests.cs
git commit -m "test(integration): add progressive UI flow tests

- Test Idle -> Processing -> Idle workflow
- Test state transition matrix validation
- Verify all valid/invalid transitions"
```

---

### Task 25: Final cleanup and documentation

**Files:**
- Modify: `src/GenSubtitle.App/Views/MainWindow.xaml`
- Create: `docs/progressive-ui-architecture.md`

- [ ] **Step 1: Remove console log section from MainWindow (temporary)**

Comment out or remove the console log Card (Grid.Row="3"):

```xml
<!-- Temporarily disabled - will be removed in Phase 2 -->
<!-- <materialDesign:Card Grid.Row="3" Padding="8" Margin="0">...</materialDesign:Card> -->
```

Update RowDefinitions to remove the last row:

```xml
<Grid.RowDefinitions>
    <RowDefinition Height="Auto" />
    <RowDefinition Height="*" />
    <RowDefinition Height="Auto" />
    <!-- <RowDefinition Height="180" /> -->
</Grid.RowDefinitions>
```

- [ ] **Step 2: Create architecture documentation**

```markdown
# Progressive UI Architecture

## Overview

The progressive UI automatically switches between three states based on task status:
- **Idle**: Welcome page when no tasks
- **Processing**: Progress overview when tasks are active
- **Editing**: Video player and subtitle editor for completed tasks

## State Machine

```
Idle ←→ Processing ←→ Editing
  ↑         ↓           ↓
  └─────────┴───────────┘
            ↓
          (no tasks)
```

## Key Components

### ViewStateManager
- Manages state transitions
- Validates transitions using state matrix
- Raises StateChanged events

### Views
- **IdleView**: Welcome page with import button
- **ProcessingView**: Task list and statistics
- **EditingView**: Video player and subtitle editor

### ViewModels
- **MainViewModel**: Orchestrates state transitions
- **IdleViewModel**: Handles file import
- **ProcessingViewModel**: Shows task progress
- **EditingViewModel**: Manages video playback and editing

## Data Flow

1. User imports files → IdleViewModel calls EnqueueFiles()
2. TaskQueue.Tasks collection changes → MainViewModel.OnTasksChanged()
3. ViewStateManager.TransitionTo(Processing)
4. StateChanged event fires → MainViewModel switches CurrentView
5. MainWindow ContentControl displays ProcessingView
```

- [ ] **Step 3: Commit**

```bash
git add src/GenSubtitle.App/Views/MainWindow.xaml docs/progressive-ui-architecture.md
git commit -m "docs(main): cleanup and architecture documentation

- Remove console log section (temporary)
- Add progressive UI architecture documentation
- Document state machine and data flow"
```

---

## Testing Checklist

After completing all tasks, verify:

- [ ] Application starts in Idle state
- [ ] Importing files switches to Processing state
- [ ] Task list displays with progress bars
- [ ] Selecting completed task switches to Editing state
- [ ] Video player loads selected task's file
- [ ] Subtitle editor displays segments
- [ ] Time alignment buttons work
- [ ] Return to Processing button works
- [ ] Return to Idle button clears all tasks
- [ ] Drag-and-drop file import works
- [ ] All unit tests pass
- [ ] Integration tests pass

---

## Next Steps (Phase 2)

After Phase 1 is complete:
1. Implement batch operations (select all, bulk export, bulk delete)
2. Add search and filter functionality
3. Implement task statistics dashboard
4. Add keyboard shortcuts
5. Implement auto-save with conflict resolution
