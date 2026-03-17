# Phase 1 Implementation Plan - Critical Fixes

**This document contains critical fixes for the main implementation plan. Apply these fixes before starting implementation.**

---

## Fix 1: Add Missing Using Statement to Tests

**Location:** Task 3, Step 1 (ViewStateManagerTests.cs)

**Issue:** Missing using statement for TaskStatus alias

**Fix:** Add this line at the top of the test file after the other using statements:

```csharp
using CoreTaskStatus = GenSubtitle.Core.Models.TaskStatus;
```

---

## Fix 2: Fix RelayCommand Constructor Usage

**Location:** Tasks 5, 7, 13, 17, 18 (ViewModel constructors)

**Issue:** RelayCommand requires Action without parameter, but commands are initialized with Action<object>

**Fix:** Check the existing RelayCommand implementation in the codebase first:

```bash
# Check RelayCommand signature
grep -n "public RelayCommand" src/GenSubtitle.App/ViewModels/RelayCommand.cs
```

If RelayCommand signature is `public RelayCommand(Action execute)`:

**For Task 5 (IdleViewModel):**
```csharp
public IdleViewModel(ITaskQueueService taskQueue)
{
    _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
    ImportFilesCommand = new RelayCommand(() => ImportFiles(null));
}
```

**For Task 17 (EditingViewModel - alignment methods):**
Keep as-is - alignment methods work with RelayCommand because they don't need the parameter.

---

## Fix 3: Add XAML Design-Time Namespaces

**Location:** Task 6, Step 1 (IdleView.xaml)

**Issue:** Missing design-time namespace declarations

**Fix:** Replace the UserControl tag with:

```xml
<UserControl x:Class="GenSubtitle.App.Views.IdleView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
             xmlns:vm="clr-namespace:GenSubtitle.App.ViewModels"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             d:DataContext="{d:DesignInstance vm:IdleViewModel}"
             mc:Ignorable="d"
             AllowDrop="True"
             DragEnter="OnDragEnter"
             DragLeave="OnDragLeave"
             Drop="OnDrop">
```

---

## Fix 4: Fix ViewModel Constructor Base Calls

**Location:** Tasks 7, 13 (ProcessingViewModel, EditingViewModel)

**Issue:** Incorrect `: base(taskQueue)` constructor calls

**Fix:** Remove all `: base(taskQueue)` calls:

**Task 7 - ProcessingViewModel:**
```csharp
public ProcessingViewModel(ITaskQueueService taskQueue)
{
    _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
    // NO base() call - ObservableObject has parameterless constructor
}
```

**Task 13 - EditingViewModel:**
```csharp
public EditingViewModel(ITaskQueueService taskQueue)
{
    _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
    // NO base() call
}
```

---

## Fix 5: Fix EditingView Grid Layout

**Location:** Task 14, Step 1 (EditingView.xaml)

**Issue:** Timeline slider and player controls both use Grid.Row="1" causing overlap

**Fix:** Replace the Grid.RowDefinitions and restructure:

```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="*"/>          <!-- Video player -->
        <RowDefinition Height="Auto"/>       <!-- Player controls -->
        <RowDefinition Height="Auto"/>       <!-- Timeline slider -->
        <RowDefinition Height="*"/>          <!-- Subtitle editor -->
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
    <StackPanel Grid.Row="2" Orientation="Vertical" Margin="150,8,150,8">
        <Slider x:Name="TimelineSlider" Minimum="0" Maximum="1" ValueChanged="OnTimelineChanged"/>
        <TextBlock x:Name="TimeDisplay" Text="00:00 / 00:00" HorizontalAlignment="Center" FontSize="12" Foreground="{DynamicResource MaterialDesignBodyLight}" Margin="0,4,0,0"/>
    </StackPanel>

    <!-- Subtitle Editor (placeholder for now) -->
    <materialDesign:Card Grid.Row="3" Margin="8">
        <ScrollViewer VerticalScrollBarVisibility="Auto">
            <TextBlock Text="字幕编辑表格区域" HorizontalAlignment="Center" VerticalAlignment="Center" Foreground="{DynamicResource MaterialDesignBodyLight}"/>
        </ScrollViewer>
    </materialDesign:Card>
</Grid>
```

---

## Fix 6: Fix DataGrid Time Format Binding

**Location:** Task 15, Step 1 (EditingView.xaml)

**Issue:** TimeSpan StringFormat syntax incorrect

**Fix:** Replace the DataGridTextColumn definitions for Start and End:

```xml
<DataGridTextColumn Header="开始" Binding="{Binding Start}" Width="90"/>
<DataGridTextColumn Header="结束" Binding="{Binding End}" Width="90"/>
```

Note: Remove StringFormat - let TimeSpan default formatting handle it, or create a value converter if specific format needed.

---

## Fix 7: Add Complete App.xaml DataTemplate Section

**Location:** Task 12, Step 2

**Issue:** Incomplete App.xaml changes

**Fix:** Add these namespace declarations at the top of App.xaml (if not already present):

```xml
<Application x:Class="GenSubtitle.App.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="clr-namespace:GenSubtitle.App.ViewModels"
             xmlns:v="clr-namespace:GenSubtitle.App.Views">
```

Then add these DataTemplates in `<Application.Resources>` (after existing resources):

```xml
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
```

---

## Fix 8: Fix Timer Memory Leak

**Location:** Task 14, Step 2 (EditingView.xaml.cs)

**Issue:** DispatcherTimer never stopped

**Fix:** Add to the constructor:

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

    // Add cleanup
    this.Unloaded += (s, e) => _timer.Stop();
}
```

---

## Fix 9: Fix Drag-and-Drop Type Check

**Location:** Task 23, Step 2 (IdleView.xaml.cs)

**Issue:** Wrong type check in OnDrop handler

**Fix:**

```csharp
private void OnDrop(object sender, DragEventArgs e)
{
    if (e.Data.GetDataPresent(DataFormats.FileDrop) && DataContext is IdleViewModel vm)
    {
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        if (files?.Length > 0)
        {
            vm.ImportFilesCommand.Execute(files);
        }
    }
    e.Handled = true;
}
```

---

## Fix 10: Fix Integration Test Moq Setup

**Location:** Task 24, Step 1 (ProgressiveUIIntegrationTests.cs)

**Issue:** Missing Moq setup for Tasks.Count

**Fix:** In the first test, after creating the ObservableCollection:

```csharp
public void CompleteWorkflow_IdleToProcessingToIdle()
{
    // Arrange
    var mockTaskQueue = new Mock<ITaskQueueService>();
    var logger = new ConsoleLogger();
    var manager = new ViewStateManager(mockTaskQueue.Object, logger);
    var observableCollection = new System.Collections.ObjectModel.ObservableCollection<TaskItemViewModel>();

    mockTaskQueue.Setup(q => q.Tasks).Returns(observableCollection);
    mockTaskQueue.Setup(q => q.Tasks.Count).Returns(observableCollection.Count); // ADD THIS

    // ... rest of test
}
```

---

## Pre-Implementation Checklist

Before starting implementation:

- [ ] Run `mkdir -p src/GenSubtitle.App/Core src/GenSubtitle.App/Services src/GenSubtitle.Tests/ViewModels`
- [ ] Verify RelayCommand signature: `grep -A 5 "public RelayCommand" src/GenSubtitle.App/ViewModels/RelayCommand.cs`
- [ ] Verify ObservableObject signature: `grep -A 3 "public class ObservableObject" src/GenSubtitle.App/ViewModels/ObservableObject.cs`
- [ ] Check if RaisePropertyChanged uses CallerMemberName: `grep "RaisePropertyChanged" src/GenSubtitle.App/ViewModels/ObservableObject.cs`
- [ ] Verify TaskQueueViewModel.EnqueueFiles signature matches ITaskQueueService

---

## Updated Task Order Recommendation

**Phase 1A should be completed in this exact order:**

1. Task 0: Create directories (Prerequisites)
2. Task 1: ViewState enum
3. Task 2: ITaskQueueService interface
4. **Task 2.5: Check RelayCommand signature and adjust plan if needed**
5. Task 3: ViewStateManager (apply Fix 1)
6. Task 4: ConsoleLogger
7. Task 5: IdleViewModel (apply Fix 2)
8. Task 6: IdleView XAML (apply Fix 3)

**Phase 1B continues with:**

9. Task 7: ProcessingViewModel (apply Fix 4)
10. Task 8: ProcessingView XAML
11. Task 9: EditingViewModel (apply Fix 4)
12. Task 10: EditingView XAML
13. Task 11: MainViewModel integration
14. Task 12: MainWindow + App.xaml (apply Fix 7)
15. Task 13: Processing task list (apply Fix 10 for RaisePropertyChanged)
16. Task 14: Video player (apply Fix 5, Fix 8)
17. Task 15: Subtitle editor (apply Fix 6)
18. Task 16: Auto-transition
19. Task 17: Navigation buttons (apply Fix 2)
20. Task 18: Editing navigation (apply Fix 2)
21. Task 19: Time alignment (apply Fix 2)
22. Task 20: Video loading
23. Task 21: Task selection highlighting
24. Task 22: Error handling
25. Task 23: Drag-and-drop (apply Fix 9)
26. Task 24: Integration tests (apply Fix 10)
27. Task 25: Documentation

---

## Verification Commands

Run these commands after each phase to verify:

**After Phase 1A:**
```bash
dotnet build src/GenSubtitle.App/GenSubtitle.App.csproj
dotnet test src/GenSubtitle.Tests/GenSubtitle.Tests.csproj --filter "FullyQualifiedName~(ViewStateManager|IdleViewModel)"
```

**After Phase 1B:**
```bash
dotnet build src/GenSubtitle.App/GenSubtitle.App.csproj
dotnet test src/GenSubtitle.Tests/GenSubtitle.Tests.csproj
dotnet run --project src/GenSubtitle.App/GenSubtitle.App.csproj
```

---

## Common Build Errors and Solutions

**Error: "The type or namespace name 'Core' could not be found"**
- Solution: Verify ViewState.cs was created in `src/GenSubtitle.App/Core/` directory
- Check namespace is `namespace GenSubtitle.App.Core;`

**Error: "RelayCommand" has no constructor taking 'Action<object>'**
- Solution: Apply Fix 2 above - wrap in lambda: `new RelayCommand(() => Method(null))`

**Error: "The name 'InitializeComponent' does not exist in the current context"**
- Solution: Verify XAML build action is set to `Page` (should be automatic)

**Error: "Cannot find source for binding with reference 'RelayCommand'"**
- Solution: Verify all ViewModels have public ICommand properties with { get; }

**Error: Tests fail with "Object reference not set to an instance of an object"**
- Solution: Apply Fix 10 - ensure Moq setups include `.Count` property
