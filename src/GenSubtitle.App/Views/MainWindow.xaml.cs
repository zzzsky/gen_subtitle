using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using GenSubtitle.App.ViewModels;
using GenSubtitle.App.Views;
using GenSubtitle.Core.Models;
using Microsoft.Win32;

namespace GenSubtitle.App.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly DispatcherTimer _timer;
    private bool _isSeeking;
    private bool _wasPlaying;
    private bool _isPlaying;
    private bool _queueVisible = true;
    private bool _previewVisible = true;
    private bool _autoScrollLog = true;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        // Initialize notification service with main window
        _viewModel.Queue.SetMainWindow(this);

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _timer.Tick += OnTick;

        // TODO: Video player moved to EditingView - remove old Player references
        // Player.MediaOpened += (_, _) =>
        // {
        //     if (Player.NaturalDuration.HasTimeSpan)
        //     {
        //         TimelineSlider.Maximum = Player.NaturalDuration.TimeSpan.TotalSeconds;
        //     }
        // };
    }

    private void OnImportFiles(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Video Files|*.mp4;*.mkv;*.mov;*.avi|All Files|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            _viewModel.EnqueueFiles(dialog.FileNames);
        }
    }

    private async void OnExportSelected(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedTask is null)
        {
            MessageBox.Show("No task selected.", "Export", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!_viewModel.SelectedTask.CanExport)
        {
            MessageBox.Show("Selected task is not completed.", "Export", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var options = new ExportOptions();
            var dialog = new ExportWindow(options) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                await ExportWithProgressAsync(_viewModel.SelectedTask, options);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    private void OnOpenSettings(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsWindow(_viewModel.Settings) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            _viewModel.SaveSettings();
        }
    }

    private void OnExit(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnRunDiagnostics(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Diagnostics not implemented yet.", "Diagnostics", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnAbout(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("GenSubtitle\nBilingual subtitle generator", "About", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnOpenLogFolder(object sender, RoutedEventArgs e)
    {
        var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GenSubtitle", "Logs");
        Directory.CreateDirectory(logDir);
        Process.Start(new ProcessStartInfo
        {
            FileName = logDir,
            UseShellExecute = true
        });
    }

    private void OnToggleQueue(object sender, RoutedEventArgs e)
    {
        // TODO: Old UI feature - UI panels removed in Phase 1
        // _queueVisible = !_queueVisible;
        // QueuePanel.Visibility = _queueVisible ? Visibility.Visible : Visibility.Collapsed;
        // QueueColumn.Width = _queueVisible ? new GridLength(300) : new GridLength(0);
        // QueueGapColumn.Width = _queueVisible ? new GridLength(12) : new GridLength(0);
    }

    private void OnTogglePreview(object sender, RoutedEventArgs e)
    {
        // TODO: Old UI feature - UI panels removed in Phase 1
        // _previewVisible = !_previewVisible;
        // PreviewPanel.Visibility = _previewVisible ? Visibility.Visible : Visibility.Collapsed;
        // PreviewColumn.Width = _previewVisible ? new GridLength(2, GridUnitType.Star) : new GridLength(0);
        // PreviewGapColumn.Width = _previewVisible ? new GridLength(12) : new GridLength(0);
    }

    private void OnToggleCompact(object sender, RoutedEventArgs e)
    {
        // TODO: Old UI feature - UI panels removed in Phase 1
        // _queueVisible = !_queueVisible;
        // _previewVisible = !_previewVisible;
        // QueuePanel.Visibility = _queueVisible ? Visibility.Visible : Visibility.Collapsed;
        // PreviewPanel.Visibility = _previewVisible ? Visibility.Visible : Visibility.Collapsed;
        // QueueColumn.Width = _queueVisible ? new GridLength(300) : new GridLength(0);
        // QueueGapColumn.Width = _queueVisible ? new GridLength(12) : new GridLength(0);
        // PreviewColumn.Width = _previewVisible ? new GridLength(2, GridUnitType.Star) : new GridLength(0);
        // PreviewGapColumn.Width = _previewVisible ? new GridLength(12) : new GridLength(0);
    }

    private void OnTaskSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        // TODO: Video player moved to EditingView
        // if (_viewModel.SelectedTask is null) { return; }
        // Player.Source = new Uri(_viewModel.SelectedTask.FilePath);
        // Player.Stop();
    }

    private void OnPlay(object sender, RoutedEventArgs e)
    {
        // TODO: Video player moved to EditingView
    }

    private void OnPause(object sender, RoutedEventArgs e)
    {
        // TODO: Video player moved to EditingView
    }

    private void OnAlignStart(object sender, RoutedEventArgs e)
    {
        // TODO: Video player moved to EditingView
    }

    private void OnAlignEnd(object sender, RoutedEventArgs e)
    {
        // TODO: Video player moved to EditingView
    }

    private void OnTimelineChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // TODO: Video player moved to EditingView
    }

    private void OnSeekStart(object sender, MouseButtonEventArgs e)
    {
        // TODO: Video player moved to EditingView
    }

    private void OnSeekMove(object sender, MouseEventArgs e)
    {
        // TODO: Video player moved to EditingView
    }

    private void OnSeekEnd(object sender, MouseButtonEventArgs e)
    {
        // TODO: Video player moved to EditingView
    }

    private void OnTick(object? sender, EventArgs e)
    {
        // TODO: Video player moved to EditingView
    }

    private void SeekToMousePosition(MouseEventArgs e)
    {
        // TODO: Video player moved to EditingView
    }

    private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (IsInteractiveElement(e.OriginalSource as DependencyObject))
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void OnMinimize(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnMaximize(object sender, RoutedEventArgs e)
    {
        ToggleMaximize();
    }

    private void ToggleMaximize()
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnClearLog(object sender, RoutedEventArgs e)
    {
        _viewModel.Queue.LogText = string.Empty;
    }

    private async void OnExportTaskCommand(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is not TaskItemViewModel task)
        {
            return;
        }

        if (!task.CanExport)
        {
            MessageBox.Show("Selected task is not completed.", "Export", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var options = new ExportOptions();
            var dialog = new ExportWindow(options) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                await ExportWithProgressAsync(task, options);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnExportTaskCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = e.Parameter is TaskItemViewModel task && task.CanExport;
    }

    private void OnDeleteTaskCommand(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is not TaskItemViewModel task)
        {
            return;
        }

        if (task.Status is GenSubtitle.Core.Models.TaskStatus.Completed)
        {
            var dialog = new ConfirmDeleteWindow { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                _viewModel.Queue.DeleteTask(task, dialog.DeleteFiles, true);
            }
            return;
        }

        var result = MessageBox.Show("任务进行中，是否删除", "Delete Task", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        _viewModel.Queue.DeleteTask(task, true, true);
    }

    private void OnDeleteTaskCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = e.Parameter is TaskItemViewModel;
    }


    private async Task ExportWithProgressAsync(TaskItemViewModel task, ExportOptions options)
    {
        var outputDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GenSubtitle", Path.GetFileNameWithoutExtension(task.FilePath));
        var vm = new ExportProgressViewModel(task.FileName, outputDir);
        var dialog = new ExportProgressWindow(vm) { Owner = this };
        using var exportCts = new CancellationTokenSource();
        var exportTask = _viewModel.ExportTaskAsync(task, options, progress =>
        {
            Dispatcher.Invoke(() => vm.Progress = progress);
        }, exportCts.Token);

        _ = exportTask.ContinueWith(_ =>
        {
            Dispatcher.Invoke(() => vm.IsCompleted = true);
        });

        dialog.ShowDialog();
        if (!vm.IsCompleted)
        {
            exportCts.Cancel();
        }
        try
        {
            await exportTask;
        }
        finally
        {
            vm.IsCompleted = true;
        }
    }

    private void OnLogTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_autoScrollLog)
        {
            LogBox.ScrollToEnd();
        }
    }

    private void OnLogScrollChanged(object sender, System.Windows.Controls.ScrollChangedEventArgs e)
    {
        var atBottom = Math.Abs(e.VerticalOffset + e.ViewportHeight - e.ExtentHeight) < 1.0;
        _autoScrollLog = atBottom;
    }

    private void OnLogLoaded(object sender, RoutedEventArgs e)
    {
        var scrollViewer = FindScrollViewer(LogBox);
        if (scrollViewer != null)
        {
            scrollViewer.ScrollChanged += OnLogScrollChanged;
        }
    }

    private static System.Windows.Controls.ScrollViewer? FindScrollViewer(DependencyObject root)
    {
        if (root is System.Windows.Controls.ScrollViewer viewer)
        {
            return viewer;
        }

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            var result = FindScrollViewer(child);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static bool IsInteractiveElement(DependencyObject? source)
    {
        while (source != null)
        {
            if (source is System.Windows.Controls.MenuItem or System.Windows.Controls.Menu or System.Windows.Controls.Button)
            {
                return true;
            }

            source = System.Windows.Media.VisualTreeHelper.GetParent(source);
        }

        return false;
    }
}
