using System;
using System.Windows;

namespace GenSubtitle.App.Services;

/// <summary>
/// Service for showing system notifications and alerts
/// </summary>
public class NotificationService
{
    private readonly Window _mainWindow;

    public NotificationService(Window mainWindow)
    {
        _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
    }

    /// <summary>
    /// Show notification when all tasks are completed
    /// </summary>
    public void ShowTaskCompletionNotification(int totalTasks, int successfulTasks, int failedTasks)
    {
        var title = "GenSubtitle - 任务完成";
        var message = $"处理完成：{successfulTasks}/{totalTasks} 成功";
        if (failedTasks > 0)
        {
            message += $"，{failedTasks} 失败";
        }

        ShowSystemNotification(title, message);
        PlayNotificationSound();
        FlashWindow();
    }

    /// <summary>
    /// Show a system notification
    /// </summary>
    private void ShowSystemNotification(string title, string message)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // For now, show message box
                // In production, could use Windows.UI.Notifications for toast notifications
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to show notification: {ex.Message}");
        }
    }

    /// <summary>
    /// Play a notification sound
    /// </summary>
    private void PlayNotificationSound()
    {
        try
        {
            // Play system beep
            Console.Beep();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to play sound: {ex.Message}");
        }
    }

    /// <summary>
    /// Flash the window to get user attention
    /// </summary>
    private void FlashWindow()
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Flash window by toggling TopMost
                for (int i = 0; i < 3; i++)
                {
                    _mainWindow.Topmost = true;
                    _mainWindow.Activate();
                    System.Threading.Thread.Sleep(200);
                    _mainWindow.Topmost = false;
                    System.Threading.Thread.Sleep(200);
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to flash window: {ex.Message}");
        }
    }
}

