using UnityEngine;

namespace EDIVE.NativeUtils
{
    public static class ComponentExtensions
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

        public static bool DestroyComponent<T>(this Component component) where T : Component
        {
            return component.gameObject.DestroyComponent<T>();
        }

        public static bool DestroyComponentImmediate<T>(this Component component) where T : Component
        {
            return component.gameObject.DestroyComponentImmediate<T>();
        }

    }
}
