using System;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
#endif

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

#if UNITY_EDITOR
        [OnInspectorGUI]
        private void DrawInfo()
        {
            if (hideFlags.HasFlag(HideFlags.NotEditable))
            {
                SirenixEditorGUI.InfoMessageBox("Component added dynamically with 'RegisterChangeSignalListener' to monitor Transform changes");
            }
        }
#endif
    }

    public static class TransformChangeMonitorExtensions
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
