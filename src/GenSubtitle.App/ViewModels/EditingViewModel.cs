using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Windows.Input;
using GenSubtitle.App.Services;
using GenSubtitle.Core.Models;
using GenSubtitle.Core.Services;

namespace GenSubtitle.App.ViewModels;

/// <summary>
/// ViewModel for the editing/video player screen
/// </summary>
public class EditingViewModel : ObservableObject
{
    private readonly ITaskQueueService _taskQueue;
    private readonly AutoSaveService _autoSaveService;
    private string? _currentVideoPath;

    public EditingViewModel(ITaskQueueService taskQueue)
    {
        _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
        // NO base() call - ObservableObject has parameterless constructor
        SelectedTask = _taskQueue.SelectedTask;
        ReturnToProcessingCommand = new RelayCommand(ReturnToProcessing);
        PlayPauseCommand = new RelayCommand(PlayPause);
        NudgeBackwardCommand = new RelayCommand(NudgeBackward);
        NudgeForwardCommand = new RelayCommand(NudgeForward);
        SaveCommand = new RelayCommand(Save, CanSave);
        ReTranslateSelectedCommand = new RelayCommand(ReTranslateSelected, CanReTranslateSelected);
        ReTranslateAllCommand = new RelayCommand(ReTranslateAll, CanReTranslateAll);
        OpenStyleEditorCommand = new RelayCommand(OpenStyleEditor);

        // Initialize auto-save service with a common auto-save directory
        var autoSaveDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GenSubtitle", "AutoSave");
        _autoSaveService = new AutoSaveService(autoSaveDir);

        // Subscribe to task changes
        _taskQueue.Tasks.CollectionChanged += (s, e) => OnTaskCollectionChanged();

        // Subscribe to property changes for auto-save
        if (SelectedTask != null)
        {
            SelectedTask.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TaskItemViewModel.Segments))
                {
                    RequestAutoSave();
                }
            };
        }

        // Load initial video if task exists
        if (_taskQueue.SelectedTask is not null)
        {
            CurrentVideoPath = _taskQueue.SelectedTask.FilePath;
        }
    }

    public TaskItemViewModel? SelectedTask { get; private set; }

    public string? CurrentVideoPath
    {
        get => _currentVideoPath;
        private set => SetProperty(ref _currentVideoPath, value);
    }

    public RelayCommand ReturnToProcessingCommand { get; }
    public RelayCommand PlayPauseCommand { get; }
    public RelayCommand NudgeBackwardCommand { get; }
    public RelayCommand NudgeForwardCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand ReTranslateSelectedCommand { get; }
    public RelayCommand ReTranslateAllCommand { get; }
    public RelayCommand OpenStyleEditorCommand { get; }

    public event EventHandler<string>? LoadVideoRequested;
    public event EventHandler? PlayPauseRequested;
    public event EventHandler<TimeSpan>? NudgeRequested;

    public void SetSelectedTask(TaskItemViewModel? task)
    {
        SelectedTask = task;
        RaisePropertyChanged(nameof(SelectedTask));
    }

    private void OnTaskCollectionChanged()
    {
        if (_taskQueue.SelectedTask is not null)
        {
            CurrentVideoPath = _taskQueue.SelectedTask.FilePath;
            LoadVideoRequested?.Invoke(this, CurrentVideoPath);
        }
    }

    private void ReturnToProcessing()
    {
        // Deselect current task to trigger state change
        _taskQueue.SelectedTask = null;
    }

    private void PlayPause()
    {
        PlayPauseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void NudgeBackward()
    {
        NudgeRequested?.Invoke(this, TimeSpan.FromSeconds(-0.1));
    }

    private void NudgeForward()
    {
        NudgeRequested?.Invoke(this, TimeSpan.FromSeconds(0.1));
    }

    private bool CanSave()
    {
        return SelectedTask != null;
    }

    private void Save()
    {
        // Manual save - clear auto-save after saving
        if (SelectedTask != null)
        {
            try
            {
                // Get the output directory (same as the video file location)
                var videoDir = Path.GetDirectoryName(SelectedTask.FilePath);
                if (string.IsNullOrEmpty(videoDir))
                {
                    videoDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                }

                var baseName = Path.GetFileNameWithoutExtension(SelectedTask.FilePath);
                var srtPath = Path.Combine(videoDir, baseName + ".srt");

                // Convert ViewModels to model segments
                var segments = SelectedTask.Segments
                    .Select(vm => vm.ToModel())
                    .ToList();

                // Write bilingual SRT file
                GenSubtitle.Core.Services.BilingualSubtitleIO.WriteBilingualSrt(srtPath, segments);

                // Clear auto-save after successful save
                _autoSaveService.ClearAutoSave(SelectedTask);

                System.Windows.MessageBox.Show(
                    $"字幕已保存到:\n{srtPath}",
                    "保存成功",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"保存失败: {ex.Message}",
                    "错误",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
    }

    private void RequestAutoSave()
    {
        if (SelectedTask != null)
        {
            _autoSaveService.RequestAutoSave(SelectedTask);
        }
    }

    /// <summary>
    /// Check for auto-saved data and load if available
    /// </summary>
    public AutoSaveData? CheckAutoSave()
    {
        if (SelectedTask != null)
        {
            return _autoSaveService.LoadAutoSave(SelectedTask);
        }
        return null;
    }

    private bool CanReTranslateSelected()
    {
        return SelectedTask?.SelectedSegment is not null;
    }

    private async void ReTranslateSelected()
    {
        if (SelectedTask?.SelectedSegment is null) return;

        try
        {
            // Get settings for API configuration
            var settingsService = new SettingsService();
            var settings = settingsService.Load();

            if (string.IsNullOrWhiteSpace(settings.QwenApiKey))
            {
                System.Windows.MessageBox.Show(
                    "请先配置 Qwen API 密钥",
                    "翻译失败",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                return;
            }

            using var httpClient = new HttpClient();
            const string qwenBeijingBaseUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1";
            var translationService = new QwenTranslationService(httpClient, settings.QwenApiKey, qwenBeijingBaseUrl, settings.QwenModel);

            // Translate only the selected segment
            var segment = SelectedTask.SelectedSegment;
            var texts = new[] { segment.SourceText };
            var translations = await translationService.TranslateAsync(texts, "auto", "zh");

            if (translations.Count > 0)
            {
                segment.ZhText = translations[0];

                // Trigger auto-save
                _autoSaveService.RequestAutoSave(SelectedTask);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"翻译失败: {ex.Message}",
                "错误",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private bool CanReTranslateAll()
    {
        return SelectedTask != null;
    }

    private async void ReTranslateAll()
    {
        if (SelectedTask == null) return;

        try
        {
            // Get settings for API configuration
            var settingsService = new SettingsService();
            var settings = settingsService.Load();

            if (string.IsNullOrWhiteSpace(settings.QwenApiKey))
            {
                System.Windows.MessageBox.Show(
                    "请先配置 Qwen API 密钥",
                    "翻译失败",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                return;
            }

            using var httpClient = new HttpClient();
            const string qwenBeijingBaseUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1";
            var translationService = new QwenTranslationService(httpClient, settings.QwenApiKey, qwenBeijingBaseUrl, settings.QwenModel);

            // Translate all segments in batches
            var segments = SelectedTask.Segments.ToList();
            var batchSize = 20;

            for (int i = 0; i < segments.Count; i += batchSize)
            {
                var batch = segments.Skip(i).Take(batchSize).ToList();
                var texts = batch.Select(s => s.SourceText).ToList();

                var translations = await translationService.TranslateAsync(texts, "auto", "zh");

                // Update ViewModels with new translations
                for (int j = 0; j < translations.Count && j < batch.Count; j++)
                {
                    batch[j].ZhText = translations[j];
                }
            }

            // Trigger auto-save
            _autoSaveService.RequestAutoSave(SelectedTask);

            System.Windows.MessageBox.Show(
                $"已完成重新翻译 {segments.Count} 条字幕",
                "翻译完成",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"翻译失败: {ex.Message}",
                "错误",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void OpenStyleEditor()
    {
        var dialog = new Views.StyleEditorWindow
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        var result = dialog.ShowDialog();
        if (result == true)
        {
            // User clicked Apply - style settings are available in dialog properties
            // For now, we could:
            // 1. Save these as default style preferences
            // 2. Apply to current task's export options
            // 3. Update the preview subtitle styling

            // TODO: Integrate with settings service to persist style preferences
            // For now, just show confirmation
            System.Windows.MessageBox.Show(
                $"样式设置已应用:\n字体: {dialog.FontFamily}\n大小: {dialog.FontSize}\n颜色: {dialog.FontColor}\n位置: {dialog.Position}",
                "样式编辑器",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
    }
}
