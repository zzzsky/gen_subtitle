namespace GenSubtitle.Core.Models;

public sealed class ExportOptions
{
    public bool BurnIn { get; set; } = false;
    public bool SoftMux { get; set; } = true;
    public string AssStyleName { get; set; } = "Default";
}
