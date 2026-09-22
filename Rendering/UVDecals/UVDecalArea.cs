// Author: František Holubec
// Created: 22.09.2026

using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Rendering.UVDecals
{
    [Serializable]
    public struct UVDecalArea
    {
        [Required]
        public UVDecalPlacement _Placement;

        [Tooltip("Mesh UV set.")]
        public UVDecalChannel _Channel;

        [Tooltip("UV 0-1.")]
        public Vector2 _Center;

        [Tooltip("Bounding box size, UV units. 0 is off, negative mirrors.")]
        public Vector2 _Size;

        [Range(-180f, 180f)]
        [Tooltip("Degrees.")]
        public float _Rotation;

        public string EditorLabel => _Placement != null ? _Placement.name : "Missing Placement";
    }
}
