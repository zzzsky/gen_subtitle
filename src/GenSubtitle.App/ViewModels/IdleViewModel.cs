using GenSubtitle.App.Services;
using Microsoft.Win32;
using System.Windows;

namespace GenSubtitle.App.ViewModels;

/// <summary>
/// ViewModel for the idle/welcome screen
/// </summary>
public class IdleViewModel : ObservableObject
{
    private readonly ITaskQueueService _taskQueue;

    public IdleViewModel(ITaskQueueService taskQueue)
    {
        _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
        WelcomeTitle = "GenSubtitle - 双语字幕生成器";
        ImportFilesCommand = new RelayCommand(() => ImportFiles(null));
    }

    public string WelcomeTitle { get; }

    public RelayCommand ImportFilesCommand { get; }

    public void ImportFiles(string[]? files)
    {
        if (files == null || files.Length == 0)
        {
            // Open file dialog
            var dialog = new OpenFileDialog
            {
                Title = "选择视频文件",
                Filter = "视频文件 (*.mp4;*.mkv;*.mov;*.avi;*.flv;*.wmv)|*.mp4;*.mkv;*.mov;*.avi;*.flv;*.wmv|所有文件 (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                files = dialog.FileNames;
            }
            else
            {
                return; // User cancelled
            }
        }

        if (files.Length > 0)
        {
            _taskQueue.EnqueueFiles(files);
        }
    }
}
