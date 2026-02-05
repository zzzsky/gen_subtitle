namespace GenSubtitle.Core.Models;

public sealed class AppSettings
{
    public string FfmpegPath { get; set; } = "ffmpeg";
    public string WhisperPath { get; set; } = "whisper";
    public string WhisperModel { get; set; } = "small";
    public bool UseGpu { get; set; } = true;
    public string DeepLApiKey { get; set; } = string.Empty;
    public string QwenApiKey { get; set; } = string.Empty;
    public string QwenModel { get; set; } = "qwen-plus";
    public List<string> EnabledLanguages { get; set; } = new() { "en", "ja", "ko", "fr", "es", "de", "ru", "it", "pt", "th" };
    public int MaxConcurrency { get; set; } = 3;
    public bool AutoTranslateEnabled { get; set; } = true;
}
