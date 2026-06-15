// Author: František Holubec
// Created: 15.06.2026

using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.UIElements.ColorPicker
{
    public static class ColorPickerUtils
    {
        private static readonly Vector3[] _Corners = new Vector3[4];

        /// <summary>
        /// Remaps the gradient's 0..1 ramp onto <paramref name="handleArea"/> inset within the
        /// gradient image, so the (clamped) border colors extend into the padding and a handle at
        /// value 0/1 lands on the matching color. No-op when the areas coincide or are unassigned.
        /// </summary>
        public static void ApplyInset(RawImage gradient, RectTransform handleArea)
        {
            if (gradient)
                gradient.uvRect = GetInsetUvRect(gradient, handleArea);
        }

        public static Rect GetInsetUvRect(RawImage gradient, RectTransform handleArea)
        {
            var full = new Rect(0f, 0f, 1f, 1f);
            if (!gradient || !handleArea)
                return full;

            var imageRt = gradient.rectTransform;
            var imageRect = imageRt.rect;
            if (imageRect.width <= 0f || imageRect.height <= 0f)
                return full;

            // Handle area corners expressed in the gradient image's local space.
            handleArea.GetWorldCorners(_Corners);
            var min = imageRt.InverseTransformPoint(_Corners[0]); // bottom-left
            var max = imageRt.InverseTransformPoint(_Corners[2]); // top-right

            var padMinX = (min.x - imageRect.xMin) / imageRect.width;
            var padMaxX = (imageRect.xMax - max.x) / imageRect.width;
            var padMinY = (min.y - imageRect.yMin) / imageRect.height;
            var padMaxY = (imageRect.yMax - max.y) / imageRect.height;

            var spanX = 1f - padMinX - padMaxX;
            var spanY = 1f - padMinY - padMaxY;
            if (spanX <= 0f || spanY <= 0f)
                return full;

            return new Rect(-padMinX / spanX, -padMinY / spanY, 1f / spanX, 1f / spanY);
        }

        public static void Apply(RawImage target, ref Texture2D texture, Color32[] colors, Slider.Direction direction)
        {
            if (!target || colors == null || colors.Length == 0)
                return;

            var vertical = direction is Slider.Direction.BottomToTop or Slider.Direction.TopToBottom;
            var inverted = direction is Slider.Direction.TopToBottom or Slider.Direction.RightToLeft;

            var length = colors.Length;
            var width = vertical ? 1 : length;
            var height = vertical ? length : 1;

            if (texture == null || texture.width != width || texture.height != height)
            {
                if (texture)
                    Object.Destroy(texture);
                texture = new Texture2D(width, height) {wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave};
                target.texture = texture;
            }
            
            if (inverted)
            {
                var reversed = new Color32[length];
                for (var i = 0; i < length; i++)
                    reversed[i] = colors[length - 1 - i];
                colors = reversed;
            }

            texture.SetPixels32(colors);
            texture.Apply();
        }
    }
}
