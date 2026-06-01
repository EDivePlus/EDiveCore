using System;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.UIElements.Layout
{
    [Serializable]
    public class RadialFillImageVisual : IRadialElementVisual
    {
        [SerializeField]
        private Image _Image;
        
        [SerializeField]
        [Range(0f, 180f)]
        private float _AngularPadding;

        [SerializeField]
        private bool _ClockwiseFill = true;

        [SerializeField]
        private Vector2 _Offset;

        public void Apply(RadialLayoutElement element, in RadialSliceInfo info)
        {
            if (_Image == null) return;
            if (element.transform.parent is not RectTransform layoutRect) return;

            _Image.type = Image.Type.Filled;
            _Image.fillMethod = Image.FillMethod.Radial360;
            _Image.fillOrigin = (int) Image.Origin360.Top;
            _Image.fillClockwise = _ClockwiseFill;

            var width = Mathf.Max(0f, info.Width - _AngularPadding * 2f);
            _Image.fillAmount = width / 360f;

            var rad = (90f + info.CenterAngle) * Mathf.Deg2Rad;
            var radialDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            var tangDir = new Vector2(-radialDir.y, radialDir.x);
            var localOffset = (Vector3) (tangDir * _Offset.x + radialDir * _Offset.y);

            var halfWidth = width * 0.5f;
            var rotZ = _ClockwiseFill ? info.CenterAngle + halfWidth : info.CenterAngle - halfWidth;

            _Image.rectTransform.position = layoutRect.TransformPoint(localOffset);
            _Image.rectTransform.rotation = layoutRect.rotation * Quaternion.Euler(0f, 0f, rotZ);
        }
    }
}
