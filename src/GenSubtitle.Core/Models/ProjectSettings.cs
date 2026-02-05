namespace GenSubtitle.Core.Models;

public sealed class ProjectSettings
{
    public string WhisperModel { get; set; } = "small";
    public bool UseGpu { get; set; } = true;
    public List<string> EnabledLanguages { get; set; } = new() { "en", "ja", "ko", "fr", "es", "de", "ru", "it", "pt", "th" };
}
