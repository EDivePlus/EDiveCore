using System.Collections.Generic;
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
        
    
    }
}
