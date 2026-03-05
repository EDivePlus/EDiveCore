// Author: František Holubec
// Created: 05.03.2026

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EDIVE.NativeUtils
{
    public static class GameObjectUtils
    {
        public static T Instantiate<T>(T prefab, Transform parent) where T : Object
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return (T) PrefabUtility.InstantiatePrefab(prefab, parent);
#endif
            return Object.Instantiate(prefab, parent);
        }
    }
}
