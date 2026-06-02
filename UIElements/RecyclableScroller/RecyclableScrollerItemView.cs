// Author: František Holubec
// Created: 02.06.2026

using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.UIElements.RecyclableScroller
{
    public class RecyclableScrollerItemView : MonoBehaviour
    {
        internal RecyclableScrollerItemView SourcePrefab { get; set; }
        internal LayoutElement LayoutElement { get; set; }

        public int ItemIndex { get; internal set; }

        public int DataIndex { get; internal set; }

        public bool Active { get; internal set; }

        public virtual void RefreshView() { }
    }
}
