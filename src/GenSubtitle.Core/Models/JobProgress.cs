namespace GenSubtitle.Core.Models;

public sealed class JobProgress
{
    public double Percent { get; init; }
    public string Stage { get; init; } = string.Empty;
}
