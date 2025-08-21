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
        public static void AddTransformChangeListener(this GameObject go, Action<Transform> listener)
        {
            if (go == null || listener == null)
                return;
            
            var monitor = go.GetOrAddComponent<TransformChangeMonitor>();

            monitor.hideFlags = HideFlags.NotEditable | HideFlags.DontSave;
            monitor.TransformChanged -= listener;
            monitor.TransformChanged += listener;
        }

        public static void RemoveTransformChangeListener(this GameObject go, Action<Transform> listener)
        {
            if (go == null || listener == null || !go.TryGetComponent<TransformChangeMonitor>(out var monitor))
                return;

            monitor.TransformChanged -= listener;
        }

        public static void AddChangeListener(this Transform tr, Action<Transform> listenerMethod)
        {
            tr.gameObject.AddTransformChangeListener(listenerMethod);
        }

        public static void RemoveChangeListener(this Transform tr, Action<Transform> listenerMethod)
        {
            tr.gameObject.RemoveTransformChangeListener(listenerMethod);
        }
    }
}
