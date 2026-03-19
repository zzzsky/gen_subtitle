using System.Windows;
using GenSubtitle.App.Views;

namespace GenSubtitle.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Check if this is first run
        var firstRun = CheckFirstRun();
        if (firstRun)
        {
            ShowTutorialWizard();
        }
    }

    private bool CheckFirstRun()
    {
        // TODO: Check settings or registry for first-run flag
        // For now, always return false to not show the wizard during development
        // In production, check AppSettings.FirstRun property
        return false;
    }

    private void ShowTutorialWizard()
    {
        var wizard = new TutorialWizardWindow();
        wizard.ShowDialog();
    }
}
