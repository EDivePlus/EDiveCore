// Author: František Holubec
// Created: 12.10.2026

namespace EDIVE.View.Windows
{
    // What opening a definition does while it is already open
    public enum WindowInstancePolicy
    {
        // Opens another window
        Multiple,
        // Brings the open one to front, applies the new context if given
        Single,
        // Closes the open one, opens a new one
        Replace
    }
}
