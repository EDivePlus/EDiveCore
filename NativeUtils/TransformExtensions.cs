using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace EDIVE.NativeUtils
{
    public static class TransformExtensions
    {
        public static void DestroyChildrenImmediate(this Transform t)
        {
            while (t.childCount > 0)
            {
                var child = t.GetChild(0);
                Object.DestroyImmediate(child.gameObject);
            }
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

        public static void Reset(this Transform tr)
        {
            tr.position = Vector3.zero;
            tr.rotation = Quaternion.identity;
            tr.localScale = Vector3.one;
        }
        
        public static void ResetLocal(this Transform tr)
        {
            tr.localPosition = Vector3.zero;
            tr.localRotation = Quaternion.identity;
            tr.localScale = Vector3.one;
        }
        
        public static string GetPath(this Transform transform)
        {
            if (transform == null)
                return string.Empty;

            var pathBuilder = new StringBuilder();
            var currentTransform = transform;
            while (currentTransform != null)
            {
                if (pathBuilder.Length > 0)
                    pathBuilder.Insert(0, "/");
                pathBuilder.Insert(0, currentTransform.name);
                currentTransform = currentTransform.parent;
            }
            return pathBuilder.ToString();
        }

        public static string GetPathIn(this Transform transform, Transform parent)
        {
            if (transform == null || parent == null)
                return string.Empty;

            var pathBuilder = new StringBuilder();
            var currentTransform = transform;
            while (currentTransform != null && currentTransform != parent)
            {
                if (pathBuilder.Length > 0)
                    pathBuilder.Insert(0, "/");
                pathBuilder.Insert(0, currentTransform.name);
                currentTransform = currentTransform.parent;
            }

            if (currentTransform == null)
                return string.Empty; // Not a child of the specified parent

            return pathBuilder.ToString();
        }
    }
}
