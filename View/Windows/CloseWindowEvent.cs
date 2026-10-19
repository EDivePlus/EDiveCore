// Author: František Holubec
// Created: 19.10.2026

namespace EDIVE.View.Windows
{
    // Closes the nearest window above the sender, e.g. this.SendViewEvent(new CloseWindowEvent())
    public readonly struct CloseWindowEvent
    {
        public readonly object Result;
        public readonly bool Immediate;

        public CloseWindowEvent(object result = null, bool immediate = false)
        {
            Result = result;
            Immediate = immediate;
        }
    }
}
