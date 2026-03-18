namespace GenSubtitle.App.Core;

/// <summary>
/// Represents the three possible UI states in the progressive interface
/// </summary>
public enum ViewState
{
    /// <summary>
    /// No tasks - shows welcome/guidance page
    /// </summary>
    Idle,

    /// <summary>
    /// Tasks are processing or in queue - shows progress overview
    /// </summary>
    Processing,

    /// <summary>
    /// User has selected a completed task for editing - shows video player and subtitle editor
    /// </summary>
    Editing
}
