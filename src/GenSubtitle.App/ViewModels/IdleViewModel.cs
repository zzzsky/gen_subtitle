using GenSubtitle.App.Services;

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
        // TODO: Implement file import logic
        // This will be connected to TaskQueueViewModel.EnqueueFiles
    }
}
