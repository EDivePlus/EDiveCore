// Author: František Holubec
// Created: 02.09.2026

using System;
using UnityEngine;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("Texture")]
        [Tooltip("RGB is the stamp colour, alpha its shape.")]
        public Texture2D _Texture;

        [FormerlySerializedAs("Channel")]
        [Tooltip("Which UV set of the mesh to place the stamp in.")]
        public UVDecalChannel _Channel;

        [FormerlySerializedAs("Center")]
        [Tooltip("Centre of the stamp in the chosen UV set (0-1).")]
        public Vector2 _Center;

        [FormerlySerializedAs("Size")]
        [Tooltip("Stamp size in UV units. 0 on either axis disables the stamp.")]
        public Vector2 _Size;

        [FormerlySerializedAs("Rotation")]
        [Range(-180f, 180f)]
        [Tooltip("Rotation of the stamp, counter-clockwise in UV space.")]
        public float _Rotation;

        [FormerlySerializedAs("Tint")]
        [ColorUsage(true, false)]
        [Tooltip("Multiplies the stamp; alpha scales the whole thing.")]
        public Color _Tint;

        public static UVDecal Default => new()
        {
            _Center = new Vector2(0.5f, 0.5f),
            _Size = new Vector2(0.2f, 0.2f),
            _Rotation = 0f,
            _Tint = Color.white,
        };
    }
}
