// Author: František Holubec
// Created: 07.10.2026

using EDIVE.UIElements.ProceduralUI;
using UnityEngine;

namespace EDIVE.UIElements.ProgressBars
{
    public class SDFArcProgressBar : AProgressBar
    {
        [SerializeField]
        private SDFArc _Arc;

        [SerializeField]
        private Vector2 _FillRange = new(0, 1);

        public override float Progress
        {
            get => _Arc == null ? 0f : Mathf.InverseLerp(_FillRange.x, _FillRange.y, _Arc.Value);
            set
            {
                if (_Arc)
                    _Arc.Value = Mathf.Lerp(_FillRange.x, _FillRange.y, value);
            }
        }
    }
}
