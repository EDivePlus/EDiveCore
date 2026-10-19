// Author: František Holubec
// Created: 09.10.2026

using UnityEngine;

namespace EDIVE.View.ViewTree
{
    public static class ViewTreeExtensions
    {
        // Nearest T on the logical parent GameObjects, runtime parents in play mode, designated ones in edit mode
        public static T FindInViewTree<T>(this Component component) where T : class
        {
            var node = component.FindViewNode();
            for (; node != null; node = Application.isPlaying ? node.Parent : node.GetDesignatedParent())
            {
                if (node.TryGetComponent(out T match))
                    return match;
            }
            return null;
        }

        // The node itself or the nearest one above
        public static AViewNode FindViewNode(this Component component)
        {
            var node = component as AViewNode;
            return node != null ? node : component.GetComponentInParent<AViewNode>(true);
        }

        // Links content created inactive below a group, e.g. right after Instantiate into a plain container
        public static void LinkViewTree(this Component root)
        {
            if (root != null)
                ViewGroup.LinkHierarchy(root.transform);
        }

        public static void LinkViewTree(this GameObject root)
        {
            if (root != null)
                ViewGroup.LinkHierarchy(root.transform);
        }

        // From the sender's node up the view tree, true when handled
        public static bool SendViewEvent<TEvent>(this Component sender, TEvent viewEvent)
        {
            var node = sender.FindViewNode();
            return node != null && node.SendViewEvent(viewEvent);
        }
    }
}
