// Author: František Holubec
// Created: 01.06.2026

namespace EDIVE.UIElements.Layout
{
    public readonly struct RadialSliceInfo
    {
        public readonly float CenterAngle;
        public readonly float Width;
        public readonly float Radius;
        public readonly bool RotateElements;

        public RadialSliceInfo(float centerAngle, float width, float radius, bool rotateElements)
        {
            CenterAngle = centerAngle;
            Width = width;
            Radius = radius;
            RotateElements = rotateElements;
        }
    }
}
