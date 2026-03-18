using System;
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
