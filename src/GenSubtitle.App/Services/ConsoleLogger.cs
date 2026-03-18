using System;

namespace GenSubtitle.App.Services;

/// <summary>
/// Simple console logger implementation
/// </summary>
public class ConsoleLogger : ILogger
{
    public void LogInformation(string message)
    {
        Console.WriteLine($"[INFO] {message}");
    }

    public void LogWarning(string message)
    {
        Console.WriteLine($"[WARN] {message}");
    }

    public void LogError(Exception ex, string message)
    {
        Console.WriteLine($"[ERROR] {message}");
        Console.WriteLine($"Exception: {ex.Message}");
    }
}
