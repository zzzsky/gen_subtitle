using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CoreTaskStatus = GenSubtitle.Core.Models.TaskStatus;
using GenSubtitle.Core.Models;
using GenSubtitle.Core.Services;
using GenSubtitle.App.Services;

namespace GenSubtitle.App.ViewModels;

public sealed class TaskQueueViewModel : ObservableObject
{
    private readonly JobPipelineService _pipelineService;
    private readonly TaskCacheService _cacheService = new();
    private readonly Dispatcher _dispatcher;
    private AppSettings _settings;
    private SemaphoreSlim _semaphore;
    private TaskItemViewModel? _selectedTask;
    private string _queueSummary = string.Empty;
    private string _logText = string.Empty;
    private int _nextId = 1;

    public TaskQueueViewModel(AppSettings settings)
    {
        _settings = settings;
        _pipelineService = new JobPipelineService();
        _dispatcher = Application.Current.Dispatcher;
        _semaphore = new SemaphoreSlim(Math.Max(1, _settings.MaxConcurrency));
        Tasks = new ObservableCollection<TaskItemViewModel>();
        UpdateSummary();
    }

    public ObservableCollection<TaskItemViewModel> Tasks { get; }

    public TaskItemViewModel? SelectedTask
    {
        get => _selectedTask;
        set
        {
            if (SetProperty(ref _selectedTask, value))
            {
                RaisePropertyChanged(nameof(CanExportSelected));
            }
        }
    }

    public string QueueSummary
    {
        get => _queueSummary;
        private set => SetProperty(ref _queueSummary, value);
    }

    public string LogText
    {
        get => _logText;
        set => SetProperty(ref _logText, value);
    }

    public void UpdateSettings(AppSettings settings)
    {
        _settings = settings;
        _semaphore = new SemaphoreSlim(Math.Max(1, settings.MaxConcurrency));
        Log($"Settings updated. MaxConcurrency={_settings.MaxConcurrency} AutoTranslate={_settings.AutoTranslateEnabled}");
    }

    public TaskItemViewModel? EnqueueFiles(string[] files)
    {
        TaskItemViewModel? firstTask = null;
        foreach (var file in files.Where(File.Exists))
        {
            var task = new TaskItemViewModel(this, _nextId++, file);
            task.UpdateStatus(CoreTaskStatus.Queued);
            AddTask(task);
            StartTask(task);
            if (firstTask is null)
            {
                firstTask = task;
            }
        }

        Log($"Enqueued {files.Length} file(s).");
        SaveCache();
        UpdateSummary();
        return firstTask;
    }

    public void LoadCachedTasks()
    {
        var cached = _cacheService.Load();
        foreach (var path in cached.Where(File.Exists))
        {
            var task = new TaskItemViewModel(this, _nextId++, path);
            ApplyCachedStatus(task);
            AddTask(task);
        }

        if (cached.Count > 0)
        {
            Log($"Loaded {cached.Count} cached task(s).");
        }
        UpdateSummary();
    }

    public void DeleteTask(TaskItemViewModel task, bool deleteFolder, bool force)
    {
        if (task.Status is CoreTaskStatus.Transcribing or CoreTaskStatus.Translating or CoreTaskStatus.Queued)
        {
            if (!force)
            {
                return;
            }
            CancelTask(task);
        }

        if (deleteFolder)
        {
            var folder = ResolveOutputDirectory(task.FilePath);
            if (Directory.Exists(folder))
            {
                try
                {
                    Directory.Delete(folder, true);
                }
                catch (Exception ex)
                {
                    Log($"Failed to delete cache folder: {ex.Message}");
                }
            }
        }

        RunOnUi(() => Tasks.Remove(task));
        SaveCache();
        Log($"Deleted task cache: {task.FileName}");
        UpdateSummary();
    }

    public bool CanExportSelected => SelectedTask?.Status == CoreTaskStatus.Completed;

    public void PauseTask(TaskItemViewModel task)
    {
        task.PauseRequested = true;
        task.UpdateStatus(CoreTaskStatus.Paused);
        task.CancellationTokenSource?.Cancel();
        Log($"Paused: {task.FileName}");
        UpdateSummary();
    }

    public void ResumeTask(TaskItemViewModel task)
    {
        if (task.Status != CoreTaskStatus.Paused)
        {
            return;
        }

        task.PauseRequested = false;
        task.UpdateStatus(CoreTaskStatus.Queued);
        StartTask(task);
        Log($"Resumed: {task.FileName}");
        UpdateSummary();
    }

    public void CancelTask(TaskItemViewModel task)
    {
        task.PauseRequested = false;
        task.UpdateStatus(CoreTaskStatus.Canceled);
        task.CancellationTokenSource?.Cancel();
        Log($"Canceled: {task.FileName}");
        UpdateSummary();
    }

    public void OpenFolder(TaskItemViewModel task)
    {
        var folder = ResolveOutputDirectory(task.FilePath);
        if (Directory.Exists(folder))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = folder,
                UseShellExecute = true
            });
        }
        else
        {
            Log($"Folder not found: {folder}");
        }
    }

    public Task ExportTaskAsync(TaskItemViewModel task, ExportOptions options, Action<double>? onProgress = null, CancellationToken cancellationToken = default)
    {
        return ExportTaskInternalAsync(task, options, onProgress, cancellationToken);
    }

    private void StartTask(TaskItemViewModel task)
    {
        if (task.Status == CoreTaskStatus.Canceled)
        {
            return;
        }

        task.CancellationTokenSource = new CancellationTokenSource();
        _ = Task.Run(() => ProcessTaskAsync(task, task.CancellationTokenSource.Token));
    }

    private async Task ProcessTaskAsync(TaskItemViewModel task, CancellationToken cancellationToken)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            if (_settings.AutoTranslateEnabled && string.IsNullOrWhiteSpace(_settings.QwenApiKey))
            {
                task.UpdateStatus(CoreTaskStatus.Failed, "Qwen API key not configured.");
                Log($"Failed: {task.FileName} (Qwen API key missing)");
                return;
            }

            task.UpdateStatus(CoreTaskStatus.Transcribing);
            task.UpdateProgress(0);
            UpdateSummary();
            Log($"Transcribing: {task.FileName}");

            var outputDir = ResolveOutputDirectory(task.FilePath);
            var progress = new Action<JobProgress>(p =>
            {
                task.UpdateProgress(p.Percent * 100);
                if (p.Stage == "Translating")
                {
                    task.UpdateStatus(CoreTaskStatus.Translating);
                    var ratio = (p.Percent - 0.7) / 0.3;
                    task.UpdateTranslationProgress(ratio);
                    UpdateSummary();
                }
            });

            var result = await _pipelineService.RunAsync(
                task.FilePath,
                outputDir,
                _settings,
                progress,
                line => Log($"[whisper] {line}"),
                (language, segments) =>
                {
                    Log($"Transcription done: {task.FileName} (lang={language}, segments={segments.Count})");
                    task.SetResult(new JobResult { Language = language, Segments = segments.ToList() });
                },
                (startIndex, translated) =>
                {
                    task.UpdateTranslationBatch(startIndex, translated);
                    Log($"Translated batch: {task.FileName} (start={startIndex}, count={translated.Count})");
                },
                cancellationToken);
            task.SetResult(result);
            task.UpdateProgress(100);
            task.UpdateStatus(CoreTaskStatus.Completed);
            Log($"Completed: {task.FileName}");
        }
        catch (OperationCanceledException)
        {
            if (task.PauseRequested)
            {
                task.UpdateStatus(CoreTaskStatus.Paused);
                Log($"Paused: {task.FileName}");
            }
            else
            {
                task.UpdateStatus(CoreTaskStatus.Canceled);
                Log($"Canceled: {task.FileName}");
            }
        }
        catch (Exception ex)
        {
            task.UpdateStatus(CoreTaskStatus.Failed, ex.Message);
            Log($"Failed: {task.FileName} ({ex.Message})");
            Log(ex.ToString());
        }
        finally
        {
            _semaphore.Release();
            UpdateSummary();
        }
    }

    private async Task ExportTaskInternalAsync(TaskItemViewModel task, ExportOptions options, Action<double>? onProgress, CancellationToken cancellationToken)
    {
        if (task.Segments.Count == 0)
        {
            return;
        }

        task.UpdateStatus(CoreTaskStatus.Exporting);
        task.UpdateProgress(0);
        var lastProgress = 0.0;
        void ReportProgress(double value)
        {
            var clamped = Math.Clamp(value, 0, 100);
            if (clamped < lastProgress)
            {
                clamped = lastProgress;
            }
            lastProgress = clamped;
            task.UpdateProgress(clamped);
            onProgress?.Invoke(clamped);
        }
        ReportProgress(0);
        Log($"Export started: {task.FileName}");

        var outputDir = ResolveOutputDirectory(task.FilePath);
        Directory.CreateDirectory(outputDir);

        var baseName = Path.GetFileNameWithoutExtension(task.FilePath);
        var srtPath = Path.Combine(outputDir, baseName + ".srt");
        var assPath = Path.Combine(outputDir, baseName + ".ass");

        if (!File.Exists(srtPath) || !File.Exists(assPath))
        {
            Log($"Missing subtitle files for export: {baseName}.srt / {baseName}.ass");
            throw new FileNotFoundException("Subtitle files not found for export.");
        }

        var ffmpeg = new FfmpegService(_settings.FfmpegPath);
        if (options.BurnIn)
        {
            var burnOut = Path.Combine(outputDir, baseName + "_burnin.mp4");
            await ffmpeg.BurnInAssAsync(task.FilePath, assPath, burnOut, ReportProgress, cancellationToken);
        }

        if (options.SoftMux)
        {
            var muxOut = Path.Combine(outputDir, baseName + "_soft.mp4");
            await ffmpeg.SoftMuxAsync(task.FilePath, srtPath, muxOut, ReportProgress, cancellationToken);
            ReportProgress(100);
        }

        if (!options.SoftMux)
        {
                if (options.BurnIn)
                {
                    ReportProgress(100);
                }
            }

        task.UpdateStatus(CoreTaskStatus.Completed);
        Log($"Export completed: {task.FileName}");
    }

    private void AddTask(TaskItemViewModel task)
    {
        RunOnUi(() =>
        {
            Tasks.Add(task);
            if (SelectedTask is null)
            {
                SelectedTask = task;
            }
        });
    }

    private void ApplyCachedStatus(TaskItemViewModel task)
    {
        var folder = ResolveOutputDirectory(task.FilePath);
        if (!Directory.Exists(folder))
        {
            task.UpdateStatus(CoreTaskStatus.Queued);
            return;
        }

        var baseName = Path.GetFileNameWithoutExtension(task.FilePath);
        var modelKey = NormalizeKey(_settings.WhisperModel);
        var preferredLanguages = GetPreferredLanguages(_settings.EnabledLanguages);
        var vtt = FindCacheFile(folder, baseName, "vtt", modelKey, preferredLanguages);
        var zh = FindCacheFile(folder, baseName, "zh", modelKey, preferredLanguages);
        if (!string.IsNullOrWhiteSpace(vtt))
        {
            var segments = BilingualSubtitleIO.LoadFromSrt(vtt);
            if (!string.IsNullOrWhiteSpace(zh))
            {
                BilingualSubtitleIO.ApplyTranslationFromSrt(segments, zh);
            }
            task.SetResult(new JobResult { Language = string.Empty, Segments = segments.ToList() });
            task.UpdateProgress(100);
            task.UpdateStatus(CoreTaskStatus.Completed);
        }
        else
        {
            task.UpdateStatus(CoreTaskStatus.Queued);
        }
    }

    private void SaveCache()
    {
        _cacheService.Save(Tasks.Select(t => t.FilePath));
    }

    private void UpdateSummary()
    {
        RunOnUi(() =>
        {
            var running = Tasks.Count(t => t.Status is CoreTaskStatus.Transcribing or CoreTaskStatus.Translating);
            var queued = Tasks.Count(t => t.Status == CoreTaskStatus.Queued);
            QueueSummary = $"{running}/{Tasks.Count} running · {queued} queued";
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

    private void Log(string message)
    {
        RunOnUi(() =>
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            LogText = string.IsNullOrWhiteSpace(LogText) ? line : $"{LogText}{Environment.NewLine}{line}";
        });
    }

    private static string ResolveOutputDirectory(string videoPath)
    {
        var baseName = Path.GetFileNameWithoutExtension(videoPath);
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GenSubtitle");
        return Path.Combine(root, baseName);
    }

    private static string NormalizeKey(string value)
    {
        return value.Replace('.', '_').Replace('-', '_').ToLowerInvariant();
    }

    private static IReadOnlyList<string> GetPreferredLanguages(IReadOnlyList<string>? configured)
    {
        if (configured is null || configured.Count == 0)
        {
            return new[] { "auto" };
        }

        return configured
            .Where(lang => !string.IsNullOrWhiteSpace(lang))
            .Select(NormalizeLang)
            .Distinct()
            .ToList();
    }

    private static string? FindCacheFile(string folder, string baseName, string prefix, string modelKey, IReadOnlyList<string> preferredLanguages)
    {
        if (!Directory.Exists(folder))
        {
            return null;
        }

        var pattern = $"{baseName}_{prefix}_{modelKey}_*.srt";
        var files = Directory.GetFiles(folder, pattern, SearchOption.TopDirectoryOnly);
        if (files.Length == 0)
        {
            return null;
        }

        foreach (var lang in preferredLanguages)
        {
            var match = files.FirstOrDefault(file => ExtractLangKey(file, baseName, prefix, modelKey) == lang);
            if (match != null)
            {
                return match;
            }
        }

        var autoMatch = files.FirstOrDefault(file => ExtractLangKey(file, baseName, prefix, modelKey) == "auto");
        if (autoMatch != null)
        {
            return autoMatch;
        }

        return files[0];
    }

    private static string? ExtractLangKey(string filePath, string baseName, string prefix, string modelKey)
    {
        var name = Path.GetFileNameWithoutExtension(filePath);
        var expectedPrefix = $"{baseName}_{prefix}_{modelKey}_";
        if (!name.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var lang = name.Substring(expectedPrefix.Length);
        return NormalizeLang(lang);
    }

    private static string NormalizeLang(string value)
    {
        return value.Replace('-', '_').ToLowerInvariant();
    }
}
