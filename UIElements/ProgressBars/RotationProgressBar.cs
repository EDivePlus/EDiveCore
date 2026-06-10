// Author: Radim Holub
// Created: 2026-06-01

using UnityEngine;

namespace EDIVE.UIElements.ProgressBars
{
    public class RotationProgressBar : AProgressBar
    {
        [SerializeField] private Transform _Pointer;
        [SerializeField] private Vector2 _AngleRange = new(0f, -360f);
        private float _progress;

        public override float Progress
        {
            get => _progress;
            set
            {
                _progress = Mathf.Clamp01(value);
                if (_Pointer != null)
                    _Pointer.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(_AngleRange.x, _AngleRange.y, _progress));
            }
        }
    }
}
