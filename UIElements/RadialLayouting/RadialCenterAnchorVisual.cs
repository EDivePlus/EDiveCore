using System;
using UnityEngine;

namespace EDIVE.UIElements.Layout
{
    [Serializable]
    public class RadialCenterAnchorVisual : IRadialElementVisual
    {
        [SerializeField]
        private RectTransform _Target;
        
        [SerializeField]
        private Vector2 _Offset;
        
        [SerializeField]
        [Range(-1f, 1f)]
        private float _RotationOffset;

        public void Apply(RadialLayoutElement element, in RadialSliceInfo info)
        {
            if (_Target == null) return;
            if (element.transform.parent is not RectTransform layoutRect) return;

            var rad = (90f + info.CenterAngle) * Mathf.Deg2Rad;
            var radialDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            var tangDir = new Vector2(-radialDir.y, radialDir.x);
            var localOffset = (Vector3) (tangDir * _Offset.x + radialDir * _Offset.y);

            var sliceRot = info.CenterAngle + _RotationOffset * info.Width;

            _Target.position = layoutRect.TransformPoint(localOffset);
            _Target.rotation = layoutRect.rotation * Quaternion.Euler(0f, 0f, sliceRot);
        }
    }
}
