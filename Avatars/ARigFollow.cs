// Author: František Holubec
// Created: 16.06.2025

using UnityEngine;

namespace EDIVE.Avatars
{
    public abstract class ARigFollow : MonoBehaviour
    {
        public abstract Transform HeadSource { get; set; }
        public abstract Transform LeftHandSource { get; set; }
        public abstract Transform RightHandSource { get; set; }
    }
}
