using System.Collections.Generic;
using System.Text;
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

        public static bool TryGetGameObject(this Object obj, out GameObject gameObject)
        {
            if (obj == null)
            {
                gameObject = null;
                return false;
            }

            if (obj is GameObject go)
            {
                gameObject = go;
                return true;
            }

            if (obj is Component component)
            {
                gameObject = component.gameObject;
                return true;
            }

            gameObject = null;
            return false;
        }
        
        public static bool TryGetComponent<T>(this Object obj, out T component) where T : Component
        {
            if (obj.TryGetGameObject(out var gameObject)) 
                return gameObject.TryGetComponent(out component);
            
            component = null;
            return false;

        }
    }
}
