// Author: František Holubec
// Created: 22.09.2026

using System;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Rendering.UVDecals
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class UVDecalPreset : IEquatable<UVDecalPreset>
    {
        [Required]
        public UVDecalPlacement _Placement;

        [Tooltip("Alpha is shape.")]
        public Texture2D _Texture;

        [ColorUsage(true, false)]
        [Tooltip("Alpha is strength.")]
        public Color _Tint = Color.white;

        public bool _OverrideSmoothness;

        [Range(0f, 1f)]
        [ShowIf(nameof(_OverrideSmoothness))]
        public float _Smoothness;

        [Tooltip("Scale of the max size that fits the area preserving the texture's aspect ratio. 1 is an exact fit.")]
        public Vector2 _Scale = Vector2.one;

        [Tooltip("Anchor point in the area, like RectTransform.")]
        public Vector2 _Anchor = new(0.5f, 0.5f);

        [Tooltip("Pivot point on the decal, like RectTransform.")]
        public Vector2 _Pivot = new(0.5f, 0.5f);

        [Range(-180f, 180f)]
        public float _Rotation;

        public string EditorLabel => _Placement != null ? _Placement.name : "Missing Placement";

        public bool Equals(UVDecalPreset other)
        {
            return other != null &&
                   _Placement == other._Placement &&
                   _Texture == other._Texture &&
                   _Tint == other._Tint &&
                   _OverrideSmoothness == other._OverrideSmoothness &&
                   Mathf.Approximately(_Smoothness, other._Smoothness) &&
                   _Scale == other._Scale &&
                   _Anchor == other._Anchor &&
                   _Pivot == other._Pivot &&
                   Mathf.Approximately(_Rotation, other._Rotation);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as UVDecalPreset);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_Placement, _Texture, _Tint, _OverrideSmoothness, _Scale, _Anchor, _Pivot, _Rotation);
        }
    }
}
