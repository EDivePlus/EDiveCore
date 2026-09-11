// Author: František Holubec
// Created: 02.09.2026

using System;
using EDIVE.OdinExtensions;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
#endif

namespace EDIVE.Rendering.UVDecals
{
    public enum UVDecalChannel
    {
        Primary = 0,
        Secondary = 1,
    }

    [Serializable]
    public struct UVDecal
    {
        [Tooltip("Alpha is shape.")]
        public Texture2D _Texture;
        
        [Tooltip("Mesh UV set.")]
        public UVDecalChannel _Channel;

        [Tooltip("UV 0-1.")]
        public Vector2 _Center;
        
        [Tooltip("UV units. 0 is off.")]
        [CustomValueDrawer("DrawSize")]
        public Vector2 _Size;

        [HideInInspector]
        public bool _LockRatio;
        
        [Range(-180f, 180f)]
        [Tooltip("Degrees.")]
        public float _Rotation;
        
        [ColorUsage(true, false)]
        [Tooltip("Alpha is strength.")]
        public Color _Tint;

        public bool _OverrideSmoothness;

        [Range(0f, 1f)]
        [ShowIf(nameof(_OverrideSmoothness))]
        public float _Smoothness;

#if UNITY_EDITOR
        private Vector2 DrawSize(Vector2 value, GUIContent label, Func<GUIContent, bool> callNextDrawer, InspectorProperty property)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            callNextDrawer(label);
            var sizeChanged = EditorGUI.EndChangeCheck();
            
            GUILayout.Space(2);
            var lockIcon = _LockRatio ? FontAwesomeEditorIcons.LinkSimpleSolid : FontAwesomeEditorIcons.LinkSimpleSlashSolid;
            var lockTooltip = _LockRatio ? "Unlock texture ratio" : "Lock texture ratio";
            var rect = GUILayoutUtility.GetRect(18, 18, GUIStyle.none, GUILayoutOptions.ExpandWidth(false).Width(18));
            var lockClicked = SirenixEditorGUI.IconButton(rect, lockIcon, lockTooltip);
            GUILayout.Space(2);
            EditorGUILayout.EndHorizontal();

            var locked = _LockRatio;
            if (lockClicked)
            {
                locked = !locked;
                _LockRatio = locked;
                property.MarkSerializationRootDirty();
            }

            var size = (Vector2) property.ValueEntry.WeakSmartValue;
            var typedX = (float) property.Children[0].ValueEntry.WeakSmartValue;
            var typedY = (float) property.Children[1].ValueEntry.WeakSmartValue;
            if (!Mathf.Approximately(typedX, value.x)) size.x = typedX;
            if (!Mathf.Approximately(typedY, value.y)) size.y = typedY;

            if (!locked || !(sizeChanged || lockClicked) || _Texture == null || _Texture.height == 0)
                return size;

            var ratio = (float) _Texture.width / _Texture.height;
            var yEdited = !Mathf.Approximately(size.y, value.y) && Mathf.Approximately(size.x, value.x);
            return yEdited ? new Vector2(size.y * ratio, size.y) : new Vector2(size.x, size.x / ratio);
        }
#endif
    }
}
