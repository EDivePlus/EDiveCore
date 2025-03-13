using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor
{
    public static class KeepRefreshingUtility
    {
        private const float REFRESH_FREQUENCY = 0.5f;
        
        private static float _editModeLastUpdate;   
        private static HashSet<InspectorProperty> _currentProperties = new HashSet<InspectorProperty>();
        private static float _timer;
        private static List<OdinEditor> _editors;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeSateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeSateChanged;
        }

        private static void OnPlayModeSateChanged(PlayModeStateChange changeType)
        {
            _timer = 0;
        }
        
        public static void AddProperty(InspectorProperty property)
        {
            _currentProperties.Add(property);
            RefreshEditors();
        }

        public static void RemoveProperty(InspectorProperty property)
        {
            _currentProperties.Remove(property);
            RefreshEditors();
        }

        private static void RefreshEditors()
        {
            _editors = Resources.FindObjectsOfTypeAll<OdinEditor>()
                .Where(e => _currentProperties.Any(p => p.Tree == e.Tree))
                .ToList();
        }
        
        private static void OnEditorUpdate()
        {
            if (_currentProperties.Count == 0)
                return;
            
            var deltaTime= Time.realtimeSinceStartup - _editModeLastUpdate;
            _timer += deltaTime;
            
            if (_timer >= REFRESH_FREQUENCY)
            {
                _timer = 0;
                if (_editors == null) return;
                _editors.RemoveAll(e => e == null);
                foreach (var editor in _editors)
                {
                    editor.Repaint();
                }
            }
            _editModeLastUpdate = Time.realtimeSinceStartup;
        }
    }
}
