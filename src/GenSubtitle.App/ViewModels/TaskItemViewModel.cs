using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using CoreTaskStatus = GenSubtitle.Core.Models.TaskStatus;
using GenSubtitle.Core.Models;

namespace GenSubtitle.App.ViewModels;

public sealed class TaskItemViewModel : ObservableObject
{
    private readonly TaskQueueViewModel _queue;
    private readonly Dispatcher _dispatcher;
    private CoreTaskStatus _status;
    private double _progress;
    private string _detectedLanguage = string.Empty;
    private string _errorMessage = string.Empty;
    private SubtitleSegmentViewModel? _selectedSegment;
    private string _translationPercentText = string.Empty;
    private string _translationEtaText = string.Empty;
    private DateTime? _translationStartUtc;
    private string _progressText = string.Empty;

    public TaskItemViewModel(TaskQueueViewModel queue, int id, string filePath)
    {
        _queue = queue;
        _dispatcher = Application.Current.Dispatcher;
        Id = id;
        FilePath = filePath;
        Segments = new ObservableCollection<SubtitleSegmentViewModel>();

        PauseCommand = new RelayCommand(() => _queue.PauseTask(this), () => Status == CoreTaskStatus.Transcribing || Status == CoreTaskStatus.Translating || Status == CoreTaskStatus.Queued);
        ResumeCommand = new RelayCommand(() => _queue.ResumeTask(this), () => Status == CoreTaskStatus.Paused);
        CancelCommand = new RelayCommand(() => _queue.CancelTask(this), () => Status != CoreTaskStatus.Completed && Status != CoreTaskStatus.Canceled && Status != CoreTaskStatus.Failed);
        OpenFolderCommand = new RelayCommand(() => _queue.OpenFolder(this));
    }

    public int Id { get; }

    public string FilePath { get; }

    public string FileName => Path.GetFileName(FilePath);

    public CoreTaskStatus Status
    {
        get => _status;
        private set
        {
            if (SetProperty(ref _status, value))
            {
                RaisePropertyChanged(nameof(StatusText));
                RaisePropertyChanged(nameof(IsBusy));
                RaisePropertyChanged(nameof(CanExport));
                PauseCommand.RaiseCanExecuteChanged();
                ResumeCommand.RaiseCanExecuteChanged();
                CancelCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string StatusText => Status switch
    {
        CoreTaskStatus.Queued => "Queued",
        CoreTaskStatus.Transcribing => "Voice-to-Texting",
        CoreTaskStatus.Translating => "Translating",
        CoreTaskStatus.Exporting => "Exporting",
        CoreTaskStatus.Completed => "Completed",
        CoreTaskStatus.Failed => "Failed",
        CoreTaskStatus.Paused => "Paused",
        CoreTaskStatus.Canceled => "Canceled",
        _ => Status.ToString()
    };

    public bool IsBusy => Status is CoreTaskStatus.Transcribing or CoreTaskStatus.Translating;

    public bool IsTranslating => Status == CoreTaskStatus.Translating;

    public bool IsExporting => Status == CoreTaskStatus.Exporting;

    public bool CanExport => Status == CoreTaskStatus.Completed;

    public double Progress
    {
        get => _progress;
        private set => SetProperty(ref _progress, value);
    }

    public string DetectedLanguage
    {
        get => _detectedLanguage;
        private set => SetProperty(ref _detectedLanguage, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public string TranslationPercentText
    {
        get => _translationPercentText;
        private set => SetProperty(ref _translationPercentText, value);
    }

    public string TranslationEtaText
    {
        get => _translationEtaText;
        private set => SetProperty(ref _translationEtaText, value);
    }

    public string ProgressText
    {
        get => _progressText;
        private set => SetProperty(ref _progressText, value);
    }

    public ObservableCollection<SubtitleSegmentViewModel> Segments { get; }

    public SubtitleSegmentViewModel? SelectedSegment
    {
        get => _selectedSegment;
        set => SetProperty(ref _selectedSegment, value);
    }

    internal CancellationTokenSource? CancellationTokenSource { get; set; }
    internal bool PauseRequested { get; set; }

    public RelayCommand PauseCommand { get; }
    public RelayCommand ResumeCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand OpenFolderCommand { get; }

    public void UpdateStatus(CoreTaskStatus status, string? error = null)
    {
        RunOnUi(() =>
        {
            Status = status;
            RaisePropertyChanged(nameof(IsTranslating));
            if (status == CoreTaskStatus.Translating)
            {
                _translationStartUtc ??= DateTime.UtcNow;
            }
            else
            {
                _translationStartUtc = null;
                TranslationPercentText = string.Empty;
                TranslationEtaText = string.Empty;
                ProgressText = string.Empty;
            }
            if (!string.IsNullOrWhiteSpace(error))
            {
                ErrorMessage = error;
            }
        });
    }

    public void UpdateProgress(double percent)
    {
        RunOnUi(() =>
        {
            Progress = percent;
            var clamped = Math.Clamp(percent, 0, 100);
            var stage = Status switch
            {
                CoreTaskStatus.Translating => "Translating",
                CoreTaskStatus.Exporting => "Exporting",
                _ => "Transcribing"
            };
            ProgressText = $"{stage} {Math.Round(clamped)}%";
        });
    }

    public void UpdateTranslationProgress(double ratio)
    {
        RunOnUi(() =>
        {
            var clamped = Math.Clamp(ratio, 0, 1);
            var percent = (int)Math.Round(clamped * 100);
            TranslationPercentText = $"Translating {percent}%";
            ProgressText = TranslationPercentText;

            _translationStartUtc ??= DateTime.UtcNow;

            if (_translationStartUtc.HasValue && clamped > 0.01)
            {
                var elapsed = DateTime.UtcNow - _translationStartUtc.Value;
                var remaining = TimeSpan.FromSeconds(elapsed.TotalSeconds * (1 / clamped - 1));
                TranslationEtaText = $"ETA {remaining:mm\\:ss}";
            }
            else
            {
                TranslationEtaText = "ETA --:--";
            }
        });
    }

    public void SetResult(JobResult result)
    {
        RunOnUi(() =>
        {
            DetectedLanguage = result.Language;
            Segments.Clear();
            foreach (var segment in result.Segments)
            {
                Segments.Add(new SubtitleSegmentViewModel(segment));
            }
        });
    }

    public void UpdateTranslationBatch(int startIndex, IReadOnlyList<string> translated)
    {
        RunOnUi(() =>
        {
            for (var i = 0; i < translated.Count; i++)
            {
                var index = startIndex + i;
                if (index >= 0 && index < Segments.Count)
                {
                    Segments[index].ZhText = translated[i];
                }
            }
        });
    }

    private void RunOnUi(Action action)
    {
        if (_dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            _dispatcher.Invoke(action);
        }
    }
}
