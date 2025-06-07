using System;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
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

        [OnInspectorGUI]
        private void DrawInfo()
        {
            if (hideFlags.HasFlag(HideFlags.NotEditable))
            {
                SirenixEditorGUI.InfoMessageBox("Component added dynamically with 'RegisterChangeSignalListener' to monitor Transform changes");
            }
        }
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
