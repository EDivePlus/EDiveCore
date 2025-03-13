using System.Collections.Generic;
using UnityEngine;

namespace EDIVE.NativeUtils
{
    public static class MonoBehaviourExtensions
    {
        public static T GetOrAddComponent<T>(this Component child) where T : Component
        {
            return child.TryGetComponent<T>(out var component) ? component : child.gameObject.AddComponent<T>();
        }

        public static TBase GetOrAddComponent<TBase, T>(this Component child)
            where TBase : class
            where T : Component, TBase
        {
            return child.TryGetComponent<TBase>(out var component) ? component : child.gameObject.AddComponent<T>();
        }

        public static void SortChildren(this Transform transform, System.Comparison<Transform> comparison)
        {
            var children = new List<Transform>();
            for (var i = 0; i < transform.childCount; i++)
            {
                children.Add(transform.GetChild(i));
            }

            children.Sort(comparison);
            for (var i = 0; i < children.Count; i++)
            {
                children[i].SetSiblingIndex(i);
            }
        }
    }
}
