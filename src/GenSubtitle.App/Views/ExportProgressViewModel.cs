namespace GenSubtitle.App.Views;

public sealed class ExportProgressViewModel : GenSubtitle.App.ViewModels.ObservableObject
{
    private double _progress;
    private bool _isCompleted;
    private string _progressText = string.Empty;
    private string _etaText = string.Empty;
    private DateTime? _startUtc;

    public ExportProgressViewModel(string fileName, string outputDirectory)
    {
        FileName = fileName;
        OutputDirectory = outputDirectory;
    }

    public string FileName { get; }

    public string OutputDirectory { get; }

    public double Progress
    {
        get => _progress;
        set
        {
            if (SetProperty(ref _progress, value))
            {
                UpdateProgressText(value);
            }
        }
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set => SetProperty(ref _isCompleted, value);
    }

    public string ProgressText
    {
        get => _progressText;
        private set => SetProperty(ref _progressText, value);
    }

    public string EtaText
    {
        get => _etaText;
        private set => SetProperty(ref _etaText, value);
    }

    private void UpdateProgressText(double value)
    {
        var clamped = Math.Clamp(value, 0, 100);
        ProgressText = $"{Math.Round(clamped)}%";

        if (clamped <= 0)
        {
            _startUtc = null;
            EtaText = "ETA --:--";
            return;
        }

        _startUtc ??= DateTime.UtcNow;
        if (_startUtc.HasValue && clamped < 100)
        {
            var ratio = clamped / 100.0;
            var elapsed = DateTime.UtcNow - _startUtc.Value;
            var remaining = TimeSpan.FromSeconds(elapsed.TotalSeconds * (1 / ratio - 1));
            EtaText = $"ETA {remaining:mm\\:ss}";
        }
        else
        {
            EtaText = "ETA 00:00";
        }
    }
}
