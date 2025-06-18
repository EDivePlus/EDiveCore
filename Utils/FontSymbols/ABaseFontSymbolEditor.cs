// Author: František Holubec
// Created: 17.06.2025

using System.Reflection;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.Utils.FontSymbols
{
    public abstract class ABaseFontSymbolEditor<T> : OdinEditor
    {
        // ReSharper disable once StaticMemberInGenericType
        private static MethodInfo _isGizmosAllowedForObjectMethod;

        protected override void OnEnable()
        {
            base.OnEnable();
            SceneView.duringSceneGui += DrawAnchorsOnSceneView;
            foreach (var property in Tree.RootProperty.Children)
            {
                if (!typeof(FontSymbolTMPTextUI).IsAssignableFrom(property.Info.TypeOfOwner))
                    property.State.Visible = false;
            }
        }
        protected override void OnDisable()
        {
            base.OnDisable();
            SceneView.duringSceneGui -= DrawAnchorsOnSceneView;
        }

        private void DrawAnchorsOnSceneView(SceneView sceneView)
        {
            if (!target || targets.Length > 1)
                return;

            if (!sceneView.drawGizmos || !IsGizmosAllowedForObject(target))
                return;

            var graphic = (Graphic) target;

            var gui = graphic.rectTransform;
            var ownSpace = gui.transform;
            var rectInOwnSpace = gui.rect;

            Handles.color = Handles.UIColliderHandleColor;
            DrawRect(rectInOwnSpace, ownSpace, graphic.raycastPadding);
        }

        private static void DrawRect(Rect rect, Transform space, Vector4 offset)
        {
            var p0 = space.TransformPoint(new Vector2(rect.x + offset.x, rect.y + offset.y));
            var p1 = space.TransformPoint(new Vector2(rect.x + offset.x, rect.yMax - offset.w));
            var p2 = space.TransformPoint(new Vector2(rect.xMax - offset.z, rect.yMax - offset.w));
            var p3 = space.TransformPoint(new Vector2(rect.xMax - offset.z, rect.y + offset.y));

            Handles.DrawLine(p0, p1);
            Handles.DrawLine(p1, p2);
            Handles.DrawLine(p2, p3);
            Handles.DrawLine(p3, p0);
        }

        private static bool IsGizmosAllowedForObject(Object target)
        {
            _isGizmosAllowedForObjectMethod ??= typeof(EditorGUIUtility).GetMethod("IsGizmosAllowedForObject", BindingFlags.NonPublic | BindingFlags.Static);
            if (_isGizmosAllowedForObjectMethod != null)
                return (bool) _isGizmosAllowedForObjectMethod.Invoke(null, new object[]{target});
            return true;
        }

    }
}
