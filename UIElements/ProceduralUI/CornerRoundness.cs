// Author: Michal Petr
// Created: 22.09.2026

using System;
using UnityEngine;

#if UNITY_EDITOR
using EDIVE.OdinExtensions.Editor;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
#endif

namespace EDIVE.UIElements.ProceduralUI
{
    [Serializable]
    public struct CornerRoundness
    {
        [SerializeField]
        private RoundnessMode _Mode;

        [SerializeField]
        private float _Radius;

        [SerializeField]
        private float _TopLeft;

        [SerializeField]
        private float _TopRight;

        [SerializeField]
        private float _BottomRight;

        [SerializeField]
        private float _BottomLeft;

        public RoundnessMode Mode { get => _Mode; set => _Mode = value; }
        public float Radius { get => _Radius; set => _Radius = Mathf.Max(0f, value); }
        public float TopLeft { get => _TopLeft; set => _TopLeft = Mathf.Max(0f, value); }
        public float TopRight { get => _TopRight; set => _TopRight = Mathf.Max(0f, value); }
        public float BottomRight { get => _BottomRight; set => _BottomRight = Mathf.Max(0f, value); }
        public float BottomLeft { get => _BottomLeft; set => _BottomLeft = Mathf.Max(0f, value); }

        public static CornerRoundness Circle => new() { _Mode = RoundnessMode.Circle };
        public static CornerRoundness Uniform(float radius) => new() { _Mode = RoundnessMode.Uniform, _Radius = Mathf.Max(0f, radius) };
        public static CornerRoundness PerCorner(float topLeft, float topRight, float bottomRight, float bottomLeft) => new()
        {
            _Mode = RoundnessMode.PerCorner,
            _TopLeft = Mathf.Max(0f, topLeft),
            _TopRight = Mathf.Max(0f, topRight),
            _BottomRight = Mathf.Max(0f, bottomRight),
            _BottomLeft = Mathf.Max(0f, bottomLeft)
        };

        // Component order matches the shader: x = top right, y = bottom right, z = top left, w = bottom left
        public Vector4 Resolve(float width, float height)
        {
            switch (_Mode)
            {
                case RoundnessMode.Circle:
                    var max = Mathf.Min(width, height) * 0.5f;
                    return new Vector4(max, max, max, max);
                case RoundnessMode.Uniform:
                    return new Vector4(_Radius, _Radius, _Radius, _Radius);
                default:
                    return new Vector4(_TopRight, _BottomRight, _TopLeft, _BottomLeft);
            }
        }
    }

#if UNITY_EDITOR
    public class CornerRoundnessDrawer : OdinValueDrawer<CornerRoundness>
    {
        private const float FIELD_WIDTH = 56f;
        private const float PREVIEW_SIZE = 56f;
        private const float MIN_PREVIEW_ASPECT = 0.4f;
        private const float GAP = 6f;
        private static readonly Color PREVIEW_COLOR = new(0.62f, 0.62f, 0.62f);

        private RoundnessMode[] _modes;
        private GUIContent[] _modeContents;

        private enum Corner { TopLeft, TopRight, BottomRight, BottomLeft }

        // The owning graphic or modifier decides between round and chamfered corners
        private ShapeStyle Style => (Property.Tree.WeakTargets[0] as IShapeStyleProvider)?.ShapeStyle ?? ShapeStyle.Round;

        protected override void Initialize()
        {
            _modes = EnumToggleButtonsGUI.GetValues<RoundnessMode>();
            _modeContents = EnumToggleButtonsGUI.GetContents(typeof(RoundnessMode));
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            var value = ValueEntry.SmartValue;

            EditorGUI.BeginChangeCheck();
            value.Mode = DrawModeButtons(value.Mode);

            switch (value.Mode)
            {
                case RoundnessMode.Uniform:
                    value.Radius = EditorGUILayout.FloatField(Style == ShapeStyle.Chamfer ? "Chamfer" : "Radius", value.Radius);
                    break;
                case RoundnessMode.PerCorner:
                    DrawCorners(ref value);
                    break;
            }

            if (EditorGUI.EndChangeCheck())
                ValueEntry.SmartValue = value;
        }

        private RoundnessMode DrawModeButtons(RoundnessMode current)
        {
            var rect = EditorGUI.PrefixLabel(EditorGUILayout.GetControlRect(), GUIHelper.TempContent("Roundness"));
            var index = EnumToggleButtonsGUI.Draw(rect, Array.IndexOf(_modes, current), _modeContents);
            return index >= 0 ? _modes[index] : current;
        }

        private void DrawCorners(ref CornerRoundness value)
        {
            var rect = EditorGUILayout.GetControlRect(false, PREVIEW_SIZE);
            var lineHeight = EditorGUIUtility.singleLineHeight;
            EditorGUI.LabelField(new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, lineHeight), "Corners");

            var content = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y, rect.width - EditorGUIUtility.labelWidth, rect.height);
            GetPreviewSize(out var previewWidth, out var previewHeight, out var scale);
            var preview = new Rect(content.x + FIELD_WIDTH + GAP, content.y + (PREVIEW_SIZE - previewHeight) * 0.5f, previewWidth, previewHeight);
            var maxRadius = Mathf.Min(previewWidth, previewHeight) * 0.5f;

            var leftX = content.x;
            var rightX = preview.xMax + GAP;
            var topY = content.y;
            var bottomY = content.yMax - lineHeight;

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            value.TopLeft = DrawCornerField(new Rect(leftX, topY, FIELD_WIDTH, lineHeight), Corner.TopLeft, value.TopLeft);
            value.TopRight = DrawCornerField(new Rect(rightX, topY, FIELD_WIDTH, lineHeight), Corner.TopRight, value.TopRight);
            value.BottomRight = DrawCornerField(new Rect(rightX, bottomY, FIELD_WIDTH, lineHeight), Corner.BottomRight, value.BottomRight);
            value.BottomLeft = DrawCornerField(new Rect(leftX, bottomY, FIELD_WIDTH, lineHeight), Corner.BottomLeft, value.BottomLeft);

            EditorGUI.indentLevel = indent;

            var radii = new Vector4(
                Mathf.Min(value.TopLeft * scale, maxRadius),
                Mathf.Min(value.TopRight * scale, maxRadius),
                Mathf.Min(value.BottomRight * scale, maxRadius),
                Mathf.Min(value.BottomLeft * scale, maxRadius));
            DrawPreview(preview, radii, Style);

            if (rect.Contains(Event.current.mousePosition))
                GUIHelper.RequestRepaint();
        }

        private float DrawCornerField(Rect rect, Corner corner, float value)
        {
            var controlName = Property.Path + "." + corner;
            GUI.SetNextControlName(controlName);
            var result = SirenixEditorFields.FloatField(rect, new GUIContent(string.Empty, ObjectNames.NicifyVariableName(corner.ToString())), value);
            return result;
        }

        // radii order: top left, top right, bottom right, bottom left
        private static void DrawPreview(Rect rect, Vector4 radii, ShapeStyle style)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            if (style == ShapeStyle.Round)
            {
                GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, PREVIEW_COLOR, Vector4.zero, radii);
                return;
            }

            var previous = Handles.color;
            Handles.color = PREVIEW_COLOR;
            Handles.DrawAAConvexPolygon(
                new Vector3(rect.xMin, rect.yMin + radii.x), new Vector3(rect.xMin + radii.x, rect.yMin),
                new Vector3(rect.xMax - radii.y, rect.yMin), new Vector3(rect.xMax, rect.yMin + radii.y),
                new Vector3(rect.xMax, rect.yMax - radii.z), new Vector3(rect.xMax - radii.z, rect.yMax),
                new Vector3(rect.xMin + radii.w, rect.yMax), new Vector3(rect.xMin, rect.yMax - radii.w));
            Handles.color = previous;
        }

        // Preview keeps the target's aspect so the radii read against the real proportions
        private void GetPreviewSize(out float width, out float height, out float scale)
        {
            var rectTransform = (Property.Tree.WeakTargets[0] as Component)?.transform as RectTransform;
            var size = rectTransform ? rectTransform.rect.size : Vector2.one;
            if (size.x <= 0f || size.y <= 0f)
                size = Vector2.one;

            var aspect = Mathf.Clamp(size.x / size.y, MIN_PREVIEW_ASPECT, 1f / MIN_PREVIEW_ASPECT);
            width = aspect >= 1f ? PREVIEW_SIZE : PREVIEW_SIZE * aspect;
            height = aspect >= 1f ? PREVIEW_SIZE / aspect : PREVIEW_SIZE;
            scale = Mathf.Min(width, height) / Mathf.Min(size.x, size.y);
        }
    }
#endif
}
