// Author: Radim Holub
// Created: 23.06.2026

using UnityEngine;

namespace EDIVE.Input.Controls
{
    public enum RigHeightMode
    {
        [Tooltip("Fixed seated height")] 
        Seated,
        [Tooltip("Fixed standing height")] 
        Standing,
        [Tooltip("Real height above the floor.")] 
        Floor
    }
}
