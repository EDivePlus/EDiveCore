// Author: František Holubec
// Created: 22.10.2025

using System;
using UnityEngine;

namespace EDIVE.Procedural.MeshScaling
{
    [Serializable]
    public abstract class AMeshSliceScalerComponent
    {
        public abstract bool TryCalculateBounds(Transform root, out Bounds bounds);
        public abstract bool Recalculate(MeshSliceScaleDetails details, Component container, Transform root, bool force = false);
    }
}
