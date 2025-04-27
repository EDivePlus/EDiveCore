using System.Collections.Generic;
using UnityEngine;

namespace EDIVE.NativeUtils
{
    public static class GameObjectExtensions
    {
        public static T GetOrAddComponent<T>(this GameObject gameObj) where T : Component
        {
            return gameObj.TryGetComponent<T>(out var component) ? component : gameObj.AddComponent<T>();
        }

        public static bool DestroyComponentImmediate<T>(this GameObject gameObj) where T : Component
        {
            if (gameObj.TryGetComponent<T>(out var component))
            {
                Object.DestroyImmediate(component);
                return true;
            }

            return false;
        }

        public static bool DestroyComponent<T>(this GameObject gameObj) where T : Component
        {
            if (gameObj.TryGetComponent<T>(out var component))
            {
                Object.Destroy(component);
                return true;
            }
            return false;
        }

        public static void SetAllActive(this IEnumerable<GameObject> gameObjects, bool active)
        {
            foreach (var gameObject in gameObjects)
            {
                if(gameObject)
                    gameObject.SetActive(active);
            }
        }
    }
}
