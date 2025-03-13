using System;
using UnityEngine;

namespace EDIVE.NativeUtils
{
    public class TransformChangeMonitor : MonoBehaviour
    {
        public event Action<Transform> TransformChanged;
   
        private void LateUpdate()
        {
            if (!transform.hasChanged) 
                return;
            
            TransformChanged?.Invoke(transform);
            transform.hasChanged = false;
        }
    }

    public static class TransformChangeMonitorExtension
    {
        public static void RegisterChangeSignalListener(this Transform tr, Action<Transform> listenerMethod)
        {
            var monitor = tr.TryGetComponent<TransformChangeMonitor>(out var component) 
                ? component 
                : tr.gameObject.AddComponent<TransformChangeMonitor>();
            
            monitor.hideFlags = HideFlags.NotEditable | HideFlags.DontSave;
            monitor.TransformChanged -= listenerMethod;
            monitor.TransformChanged += listenerMethod;
        }

        public static void UnregisterChangeSignalListener(this Transform tr, Action<Transform> listenerMethod)
        {
            if (tr.TryGetComponent<TransformChangeMonitor>(out var monitor))
            {
                monitor.TransformChanged -= listenerMethod;
            }
        }
    }
}
