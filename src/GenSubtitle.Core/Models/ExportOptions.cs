namespace GenSubtitle.Core.Models;

public enum ExportFormat
{
    Srt,        // SubRip format (default)
    Vtt,        // WebVTT format
    Ass,        // Advanced SubStation Alpha
    Txt,        // Plain text without timestamps
    Bilingual   // Bilingual comparison table
}

public sealed class ExportOptions
{
    public ExportFormat Format { get; set; } = ExportFormat.Srt;
    public bool BurnIn { get; set; } = false;
    public bool SoftMux { get; set; } = true;
    public string AssStyleName { get; set; } = "Default";
}
