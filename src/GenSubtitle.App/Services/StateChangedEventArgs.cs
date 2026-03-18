using GenSubtitle.App.Core;

namespace GenSubtitle.App.Services;

/// <summary>
/// Event arguments for state changes
/// </summary>
public class StateChangedEventArgs : System.EventArgs
{
    public ViewState OldState { get; }
    public ViewState NewState { get; }

    public StateChangedEventArgs(ViewState oldState, ViewState newState)
    {
        OldState = oldState;
        NewState = newState;
    }
}
