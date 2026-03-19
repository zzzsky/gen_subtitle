using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace GenSubtitle.App.Views;

public partial class EditingView : UserControl
{
    private readonly DispatcherTimer _timer;
    private bool _isSeeking;

    public EditingView()
    {
        InitializeComponent();

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();

        // Add cleanup (Fix 8)
        this.Unloaded += (s, e) => _timer.Stop();

        Player.MediaOpened += (_, _) =>
        {
            if (Player.NaturalDuration.HasTimeSpan)
            {
                TimelineSlider.Maximum = Player.NaturalDuration.TimeSpan.TotalSeconds;
            }
        };

        // Subscribe to video load requests
        this.Loaded += (s, e) =>
        {
            if (DataContext is ViewModels.EditingViewModel vm)
            {
                vm.LoadVideoRequested += OnLoadVideoRequested;
                vm.PlayPauseRequested += OnPlayPauseRequested;
                vm.NudgeRequested += OnNudgeRequested;
                // Load initial video if path exists
                if (vm.CurrentVideoPath is not null)
                {
                    OnLoadVideoRequested(vm, vm.CurrentVideoPath);
                }
            }
        };

        this.Unloaded += (s, e) =>
        {
            if (DataContext is ViewModels.EditingViewModel vm)
            {
                vm.LoadVideoRequested -= OnLoadVideoRequested;
                vm.PlayPauseRequested -= OnPlayPauseRequested;
                vm.NudgeRequested -= OnNudgeRequested;
            }
        };

        this.DataContextChanged += (s, e) =>
        {
            if (e.OldValue is ViewModels.EditingViewModel oldVm)
            {
                oldVm.LoadVideoRequested -= OnLoadVideoRequested;
                oldVm.PlayPauseRequested -= OnPlayPauseRequested;
                oldVm.NudgeRequested -= OnNudgeRequested;
            }
            if (e.NewValue is ViewModels.EditingViewModel newVm)
            {
                newVm.LoadVideoRequested += OnLoadVideoRequested;
                newVm.PlayPauseRequested += OnPlayPauseRequested;
                newVm.NudgeRequested += OnNudgeRequested;
                if (newVm.CurrentVideoPath is not null)
                {
                    OnLoadVideoRequested(newVm, newVm.CurrentVideoPath);
                }
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

    private void OnPlayPauseRequested(object? sender, EventArgs e)
    {
        if (Player.IsLoaded && Player.CanPause)
        {
            Player.Pause();
        }
        else
        {
            Player.Play();
        }
    }

    private void OnNudgeRequested(object? sender, TimeSpan offset)
    {
        if (Player.NaturalDuration.HasTimeSpan)
        {
            var newPosition = Player.Position + offset;
            if (newPosition >= TimeSpan.Zero && newPosition <= Player.NaturalDuration.TimeSpan)
            {
                Player.Position = newPosition;
            }
        }
    }

    private void OnPlay(object sender, RoutedEventArgs e)
    {
        Player.Play();
    }

    private void OnPause(object sender, RoutedEventArgs e)
    {
        Player.Pause();
    }

    private void OnAlignStart(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.EditingViewModel vm && vm.SelectedTask?.SelectedSegment is not null)
        {
            vm.SelectedTask.SelectedSegment.Start = Player.Position;
        }
    }

    private void OnAlignEnd(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.EditingViewModel vm && vm.SelectedTask?.SelectedSegment is not null)
        {
            vm.SelectedTask.SelectedSegment.End = Player.Position;
        }
    }

    private void OnTimelineChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (Player.NaturalDuration.HasTimeSpan && _isSeeking)
        {
            Player.Position = TimeSpan.FromSeconds(e.NewValue);
        }
        TimeDisplay.Text = $"{Player.Position:hh\\:mm\\:ss} / {Player.NaturalDuration.TimeSpan:hh\\:mm\\:ss}";
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (Player.NaturalDuration.HasTimeSpan && !_isSeeking)
        {
            TimelineSlider.Value = Player.Position.TotalSeconds;
            TimeDisplay.Text = $"{Player.Position:hh\\:mm\\:ss} / {Player.NaturalDuration.TimeSpan:hh\\:mm\\:ss}";
        }
    }
}
