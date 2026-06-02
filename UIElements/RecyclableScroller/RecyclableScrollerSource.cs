// Author: František Holubec
// Created: 02.06.2026

using System;
using System.Collections.Generic;
using UnityEngine;

namespace EDIVE.UIElements.RecyclableScroller
{
    public class RecyclableScrollerSource
    {
        public readonly Func<int> GetItemCount;
        public readonly Func<int, float> GetItemSize;
        public readonly Func<int, int, RecyclableScrollerItemView> GetItemView;

        public RecyclableScrollerSource(Func<int> getItemCount, Func<int, float> getItemSize, Func<int, int, RecyclableScrollerItemView> getItemView)
        {
            GetItemCount = getItemCount;
            GetItemSize = getItemSize;
            GetItemView = getItemView;
        }

        public static RecyclableScrollerSource CreateForSinglePrefab<TItem, TData>(RecyclableScroller scroller, TItem prefab, IReadOnlyList<TData> items, Action<TItem, TData> bind, Func<TData, float> size = null)
            where TItem : RecyclableScrollerItemView
        {
            var prefabRect = (RectTransform) prefab.transform;
            return new RecyclableScrollerSource(
                () => items.Count,
                size != null
                    ? i => size(items[i])
                    : _ => scroller.ScrollDirection == RecyclableScroller.ScrollDirectionEnum.Vertical ? prefabRect.rect.height : prefabRect.rect.width,
                (dataIndex, _) =>
                {
                    var item = scroller.GetItemView(prefab);
                    bind(item, items[dataIndex]);
                    return item;
                });
        }
    }
}
