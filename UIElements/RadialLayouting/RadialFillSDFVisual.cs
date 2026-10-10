// Author: Michal Petr
// Created: 23.09.2026

using System;
using EDIVE.UIElements.ProceduralUI;
using UnityEngine;

namespace EDIVE.UIElements.Layout
{
    [Serializable]
    public class RadialFillSDFVisual : IRadialElementVisual
    {
        [SerializeField]
        private SDFArc _Arc;

        [SerializeField]
        [Range(-180f, 180f)]
        private float _AngularPadding;

        [SerializeField]
        private Vector2 _Offset;

        public void Apply(RadialLayoutElement element, in RadialSliceInfo info)
        {
            if (_Arc == null) return;
            if (element.transform.parent is not RectTransform layoutRect) return;

            var width = Mathf.Max(0f, info.Width - _AngularPadding * 2f);
            var halfWidth = width * 0.5f;

            // Layout angles grow counter-clockwise from the top, arc angles grow clockwise
            var arc = _Arc.Arc;
            var minAngle = -(info.CenterAngle + halfWidth);
            var maxAngle = -(info.CenterAngle - halfWidth);
            if (!_Arc.enabled || !Mathf.Approximately(arc.MinAngle, minAngle) || !Mathf.Approximately(arc.MaxAngle, maxAngle))
            {
                arc.MinAngle = minAngle;
                arc.MaxAngle = maxAngle;
                _Arc.Arc = arc;
                _Arc.enabled = true;
            }

            var rad = (90f + info.CenterAngle) * Mathf.Deg2Rad;
            var radialDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            var tangDir = new Vector2(-radialDir.y, radialDir.x);
            var localOffset = (Vector3) (tangDir * _Offset.x + radialDir * _Offset.y);

            _Arc.transform.position = layoutRect.TransformPoint(localOffset);
            _Arc.transform.rotation = layoutRect.rotation;
        }
    }
}
