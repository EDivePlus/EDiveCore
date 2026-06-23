using System.Collections.Generic;
using UnityEngine;

namespace EDIVE.NativeUtils
{
    public static class PreloadedAssets
    {
        private static readonly List<Object> ASSETS = new();

        public static void Register(Object asset)
        {
            if (asset != null && !ASSETS.Contains(asset))
                ASSETS.Add(asset);
        }

        public static void Unregister(Object asset)
        {
            ASSETS.Remove(asset);
        }

        public static bool TryGet<T>(out T result) where T : Object
        {
            foreach (var asset in ASSETS)
            {
                if (asset is T typed)
                {
                    result = typed;
                    return true;
                }
            }

            result = null;
            return false;
        }

        public static T Get<T>() where T : Object => TryGet<T>(out var result) ? result : null;
    }
}
