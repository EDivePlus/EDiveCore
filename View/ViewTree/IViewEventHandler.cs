// Author: František Holubec
// Created: 19.10.2026

namespace EDIVE.View.ViewTree
{
    // Handles a view event sent from below, true stops it. Nodes and components next to them.
    public interface IViewEventHandler<in TEvent>
    {
        bool HandleViewEvent(TEvent viewEvent);
    }
}
