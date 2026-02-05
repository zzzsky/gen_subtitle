namespace GenSubtitle.Core.Models;

public enum TaskStatus
{
    Queued,
    Transcribing,
    Translating,
    Exporting,
    Completed,
    Failed,
    Paused,
    Canceled
}
