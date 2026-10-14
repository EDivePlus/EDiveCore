// Author: František Holubec
// Created: 14.10.2026

namespace EDIVE.View.Windows
{
    // What Back does while the window is focused
    public enum WindowBackAction
    {
        // Back goes to the window below
        None,
        Close,
        // Stays running, see ScreenRoot.Minimize
        Minimize
    }
}
