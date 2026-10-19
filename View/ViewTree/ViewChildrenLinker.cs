// Author: František Holubec
// Created: 19.10.2026

using UnityEngine;

namespace EDIVE.View.ViewTree
{
    // On a plain container under a group, e.g. list content. Links children added here into the view tree, also inactive ones.
    [DisallowMultipleComponent]
    public class ViewChildrenLinker : MonoBehaviour
    {
        private int _linkedChildCount;

        private void OnTransformChildrenChanged()
        {
            var count = transform.childCount;
            var added = count > _linkedChildCount;
            _linkedChildCount = count;

            // Added children always come last
            if (added)
                ViewGroup.LinkHierarchy(transform.GetChild(count - 1));
        }
    }
}
