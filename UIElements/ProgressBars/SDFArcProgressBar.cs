// Author: František Holubec
// Created: 07.10.2026

using EDIVE.UIElements.ProceduralUI;
using UnityEngine;

namespace EDIVE.UIElements.ProgressBars
{
    public class SDFArcProgressBar : AProgressBar
    {
        [SerializeField]
        private SDFGraphic _Graphic;

        [SerializeField]
        private Vector2 _FillRange = new(0, 1);

        public override float Progress
        {
            get => _Graphic == null ? 0f : Mathf.InverseLerp(_FillRange.x, _FillRange.y, _Graphic.ArcValue);
            set
            {
                if (_Graphic)
                    _Graphic.ArcValue = Mathf.Lerp(_FillRange.x, _FillRange.y, value);
            }
        }
    }
}
