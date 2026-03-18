using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GenSubtitle.App.Views;

public partial class IdleView : UserControl
{
    public IdleView()
    {
        InitializeComponent();
    }

    private void OnDragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            // TODO: Add visual feedback
        }
    }

    private void OnDragLeave(object sender, DragEventArgs e)
    {
        // TODO: Remove visual feedback
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop) && DataContext is ViewModels.IdleViewModel vm)
        {
            var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
            if (files?.Length > 0)
            {
                vm.ImportFilesCommand.Execute(files);
            }
        }
        e.Handled = true;
    }
}
