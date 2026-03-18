namespace GenSubtitle.App.Services;

/// <summary>
/// Simple logger interface for dependency injection
/// </summary>
public interface ILogger
{
    void LogInformation(string message);
    void LogWarning(string message);
    void LogError(System.Exception ex, string message);
}
