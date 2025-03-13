using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ActionResolvers;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    [DrawerPriority(DrawerPriorityLevel.WrapperPriority)]
    public class OnSceneGUIAttributeDrawer : OdinAttributeDrawer<OnSceneGUIAttribute>, IDisposable
    {
        private ActionResolver _drawActionResolver;
        private static IEnumerable<OdinEditor> _editors;
        
        protected override void Initialize()
        {
            base.Initialize();
            SceneView.duringSceneGui += DrawOnSceneGUI;

            if (Property.Info.PropertyType == PropertyType.Method)
            {
                _drawActionResolver = ActionResolver.Get(Property, null);
                _editors = Resources.FindObjectsOfTypeAll<OdinEditor>()
                    .Where(e => e.Tree == Property.Tree)
                    .ToList();
            }
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (Property.Info.PropertyType == PropertyType.Method)
            {
                ActionResolver.DrawErrors(_drawActionResolver);
            }
        }

        private void DrawOnSceneGUI(SceneView sceneView)
        {
            if (Property.SerializationRoot == null)
            {
                Dispose();
                return;
            }
            
            if (_drawActionResolver.HasError) return;
            
            EditorGUI.BeginChangeCheck();
            _drawActionResolver.DoAction();
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var editor in _editors)
                {
                    editor.Repaint();
                }
            }
        }

        public void Dispose()
        {
            SceneView.duringSceneGui -= DrawOnSceneGUI;
        }
    }
}
