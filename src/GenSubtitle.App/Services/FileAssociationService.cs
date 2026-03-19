using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace GenSubtitle.App.Services;

/// <summary>
/// Service for managing Windows file associations
/// </summary>
public class FileAssociationService
{
    private const string ProgId = "GenSubtitle.VideoFile";
    private const string AppName = "GenSubtitle";

    private static readonly string[] VideoExtensions = new[]
    {
        ".mp4", ".mkv", ".mov", ".avi", ".flv", ".wmv", ".webm", ".m4v"
    };

    /// <summary>
    /// Register file associations for all video formats
    /// </summary>
    public void RegisterFileAssociations()
    {
        try
        {
            var exePath = GetExecutablePath();

            foreach (var ext in VideoExtensions)
            {
                RegisterExtension(ext, exePath);
            }

            Console.WriteLine("File associations registered successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to register file associations: {ex.Message}");
        }
    }

    /// <summary>
    /// Unregister file associations
    /// </summary>
    public void UnregisterFileAssociations()
    {
        try
        {
            foreach (var ext in VideoExtensions)
            {
                UnregisterExtension(ext);
            }

            Console.WriteLine("File associations unregistered successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to unregister file associations: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if file associations are registered
    /// </summary>
    public bool AreAssociationsRegistered()
    {
        try
        {
            var exePath = GetExecutablePath();
            var firstExt = VideoExtensions[0];

            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey($"Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FileExts\\{firstExt}");
            var progId = key?.GetValue(null) as string;

            return progId == ProgId;
        }
        catch
        {
            return false;
        }
    }

    private void RegisterExtension(string extension, string exePath)
    {
        // Register ProgID
        using (var progIdKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($"Software\\Classes\\{ProgId}"))
        {
            progIdKey.SetValue("", "Video File");
            progIdKey.SetValue("FriendlyTypeName", "Video File");

            using (var commandKey = progIdKey.CreateSubKey("shell\\open\\command"))
            {
                commandKey.SetValue("", $"\"{exePath}\" \"%1\"");
            }
        }

        // Register extension
        using (var extKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($"Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FileExts\\{extension}"))
        {
            extKey.SetValue("", ProgId);
            extKey.SetValue("Progid", ProgId);
        }
    }

    private void UnregisterExtension(string extension)
    {
        try
        {
            Microsoft.Win32.Registry.CurrentUser.DeleteSubKey($"Software\\Classes\\{ProgId}");

            using (var extKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey($"Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FileExts\\{extension}", true))
            {
                extKey?.DeleteValue("");
            }
        }
        catch
        {
            // Ignore if keys don't exist
        }
    }

    private string GetExecutablePath()
    {
        var process = Process.GetCurrentProcess();
        var mainModule = process.MainModule;
        if (mainModule != null)
        {
            return mainModule.FileName;
        }

        // Fallback
        return Environment.ProcessPath ?? "";
    }

    /// <summary>
    /// Notify Windows Explorer of file association changes
    /// </summary>
    public void NotifyExplorer()
    {
        try
        {
            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to notify explorer: {ex.Message}");
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern void SHChangeNotify(long wEventId, long uFlags, IntPtr hWnd, IntPtr lpString);
}
