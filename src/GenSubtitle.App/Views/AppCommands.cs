using System.Windows.Input;

namespace GenSubtitle.App;

public static class AppCommands
{
    public static readonly RoutedUICommand ExportTaskCommand = new();
    public static readonly RoutedUICommand DeleteTaskCommand = new();
}
