// Author: František Holubec
// Created: 08.10.2026

namespace EDIVE.View.ViewTree
{
    public enum AttachMode
    {
        // Attaches to the nearest parent ViewGroup in the hierarchy, follows when moved
        Auto,
        // Attaches to the referenced ViewGroup, can be anywhere
        Explicit,
        // Attached from code only
        Manual
    }
}
