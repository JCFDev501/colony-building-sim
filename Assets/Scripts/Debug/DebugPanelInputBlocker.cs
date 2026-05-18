using UnityEngine;

/// <summary>
/// Tracks whether the mouse is currently over a developer debug panel.
/// </summary>
public static class DebugPanelInputBlocker
{
    public static bool IsPointerOverDebugPanel { get; private set; }

    /// <summary>
    /// Clears the debug panel input block state at the beginning of a GUI pass.
    /// </summary>
    public static void ResetFrameState()
    {
        IsPointerOverDebugPanel = false;
    }

    /// <summary>
    /// Marks debug panel input as blocked for this frame.
    /// </summary>
    public static void BlockPointerInput()
    {
        IsPointerOverDebugPanel = true;
    }
}