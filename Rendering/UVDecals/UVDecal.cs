// Author: František Holubec
// Created: 02.09.2026

using System;
using EDIVE.OdinExtensions;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
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
        
        [Tooltip("UV units. 0 is off, negative mirrors.")]
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
        private Vector2 DrawSize(Vector2 value, GUIContent label, InspectorProperty property)
        {
            var ratio = _Texture != null && _Texture.height > 0 ? (float) _Texture.width / _Texture.height : 0f;
            return OdinExtensionUtils.LockableVector2Field(label, value, ratio, property, ref _LockRatio);
        }
#endif
    }
}
