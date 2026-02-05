namespace GenSubtitle.Core.Models;

public sealed class SubtitleProject
{
    public string VideoPath { get; set; } = string.Empty;
    public string AudioPath { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public List<SubtitleSegment> Segments { get; set; } = new();
    public ProjectSettings Settings { get; set; } = new();
}
