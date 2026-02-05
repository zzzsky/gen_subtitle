namespace GenSubtitle.Core.Models;

public sealed class JobResult
{
    public string Language { get; init; } = string.Empty;
    public string AudioPath { get; init; } = string.Empty;
    public List<SubtitleSegment> Segments { get; init; } = new();
}
