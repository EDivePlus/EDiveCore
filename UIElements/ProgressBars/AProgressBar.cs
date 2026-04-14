// Author: František Holubec
// Created: 14.04.2026

using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.UIElements.ProgressBars
{
    public abstract class AProgressBar : MonoBehaviour
    {
        [PropertyRange(0, 1)]
        [ShowInInspector]
        public abstract float Progress { get; set; }
    }
}
