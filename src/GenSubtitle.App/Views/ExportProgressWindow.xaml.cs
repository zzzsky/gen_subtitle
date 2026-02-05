using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace GenSubtitle.App.Views;

public partial class ExportProgressWindow : Window
{
    public ExportProgressWindow(ExportProgressViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnOpenFolder(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ExportProgressViewModel vm)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(vm.OutputDirectory) && Directory.Exists(vm.OutputDirectory))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = vm.OutputDirectory,
                UseShellExecute = true
            });
        }
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        var owner = Owner ?? Application.Current.MainWindow;
        Close();
        if (owner is null)
        {
            return;
        }

        if (owner.WindowState == WindowState.Minimized)
        {
            owner.Dispatcher.BeginInvoke(() =>
            {
                owner.WindowState = WindowState.Normal;
                owner.Activate();
                owner.Focus();
            }, DispatcherPriority.ApplicationIdle);
        }
    }
}
