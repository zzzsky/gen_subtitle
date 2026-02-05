namespace GenSubtitle.Core.Models;

public sealed class SubtitleSegment
{
    public int Id { get; set; }
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public string SourceText { get; set; } = string.Empty;
    public string ZhText { get; set; } = string.Empty;
    public float Confidence { get; set; }
}
