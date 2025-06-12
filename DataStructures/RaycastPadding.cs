// Author: František Holubec
// Created: 11.06.2025

using System;
using UnityEngine;

namespace EDIVE.DataStructures
{
    [Serializable]
    public struct RaycastPadding
    {
        [SerializeField]
        private float _Left;

        [SerializeField]
        private float _Bottom;

        [SerializeField]
        private float _Right;

        [SerializeField]
        private float _Top;

        public float Left { get => _Left; set => _Left = value; }
        public float Right { get => _Right; set => _Right = value; }
        public float Top { get => _Top; set => _Top = value; }
        public float Bottom { get => _Bottom; set => _Bottom = value; }

        public RaycastPadding(float left, float bottom, float right, float top)
        {
            _Left = left;
            _Bottom = bottom;
            _Right = right;
            _Top = top;
        }

        public static implicit operator RaycastPadding(Vector4 p) => new(p.x, p.y, p.z, p.w);
        public static implicit operator Vector4(RaycastPadding p) => new(p.Left, p.Bottom, p.Right, p.Top);
    }
}
