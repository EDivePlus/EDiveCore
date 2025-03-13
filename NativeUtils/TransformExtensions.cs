using UnityEngine;

namespace EDIVE.NativeUtils
{
    public static class TransformExtensions
    {
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
