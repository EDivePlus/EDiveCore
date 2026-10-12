// Author: František Holubec
// Created: 12.10.2026

namespace EDIVE.View.Windows
{
    public enum WindowCloseReason
    {
        // From code or the window itself, e.g. a dialog button
        Code,
        Back,
        Backdrop,
        Replaced,
        // Never opened, e.g. load failed
        Failed,
        ScreenDestroyed
    }
}
