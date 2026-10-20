// Author: František Holubec
// Created: 02.10.2025

using System;
using System.Collections.Generic;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
using UnityEditor;
#endif

namespace EDIVE.Procedural.ObjectScaling
{
    // Sizes an object in meters, modules apply it (sliced meshes, colliders).
    [ExecuteAlways]
    public class ObjectScaler : MonoBehaviour
    {
        private const float MIN_SIZE = 0.001f;

        [PropertySpace]
        [HideLabel]
        [InlineProperty]
        [SerializeField]
        [OnValueChanged(nameof(EditorRefresh), true)]
        private ObjectScaleDetails _Details = new();

        [PropertySpace]
        [SerializeReference]
        [FormerlySerializedAs("_Components")]
        [HideReferenceObjectPicker]
        [CustomValueDrawer("CustomModuleDrawer")]
        [OnValueChanged(nameof(EditorRefresh), true)]
        private List<AObjectScalerModule> _Modules = new();

        public event Action SizeChanged;

        public ObjectScaleDetails Details => _Details;
        public IReadOnlyList<AObjectScalerModule> Modules => _Modules;

        public Vector3 Size
        {
            get => _Details.Size;
            set
            {
                value = Vector3.Max(value, Vector3.one * MIN_SIZE);
                if (_Details.Size == value)
                    return;
                _Details.Size = value;
                Recalculate();
            }
        }

        // Result bounds in local space
        public Bounds ScaledBounds => SliceMath.ScaledBounds(_Details);

        public void SetSizeXY(Vector2 size) => Size = new Vector3(size.x, size.y, Size.z);

        public static Vector2 FitAspect(Vector2 maxSize, float width, float height)
        {
            if (width <= 0 || height <= 0)
                return maxSize;

            var scale = Mathf.Min(maxSize.x / width, maxSize.y / height);
            return new Vector2(width * scale, height * scale);
        }

        private void OnEnable()
        {
            RecalculateBounds();
            Recalculate();
        }

        private void OnDisable()
        {
            foreach (var module in _Modules)
                module?.Release(this);
        }

        public void RecalculateBounds()
        {
            var hasBounds = false;
            var bounds = new Bounds(Vector3.zero, Vector3.one);
            foreach (var module in _Modules)
            {
                if (module == null || !module.TryCalculateBounds(transform, out var moduleBounds))
                    continue;

                if (!hasBounds)
                {
                    bounds = moduleBounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(moduleBounds);
                }
            }

            _Details.Bounds = bounds;
            _Details.SliceMin = _Details.SliceMin.Clamp(bounds.min, bounds.max);
            _Details.SliceMax = _Details.SliceMax.Clamp(_Details.SliceMin, bounds.max);
        }

        public void Recalculate(bool force = false)
        {
            foreach (var module in _Modules)
                module?.Recalculate(this, force);
            foreach (var module in _Modules)
                module?.Postprocess(this);
            SizeChanged?.Invoke();
        }

#if UNITY_EDITOR
        [PropertyOrder(-10)]
        [ShowInInspector]
        [OnValueChanged(nameof(RefreshPreviewOriginal))]
        [Tooltip("Shows the original mesh with slice planes.")]
        private bool EditSlices { get; set; }

        private void RefreshPreviewOriginal()
        {
            if (!EditSlices)
            {
                Recalculate(true);
                return;
            }

            foreach (var module in _Modules)
                module?.SetPreviewOriginal(this, true);
        }

        private void EditorRefresh()
        {
            RecalculateBounds();
            if (EditSlices)
                RefreshPreviewOriginal();
            else
                Recalculate();
        }

        // Undo and prefab changes
        private void OnValidate()
        {
            EditorApplication.delayCall += () =>
            {
                if (this != null && isActiveAndEnabled)
                    EditorRefresh();
            };
        }

        [Button]
        private void ForceRecalculate()
        {
            RecalculateBounds();
            Recalculate(true);
        }

        [OnInspectorDispose]
        private void OnInspectorDispose()
        {
            if (!EditSlices)
                return;
            EditSlices = false;
            RefreshPreviewOriginal();
        }

        [OnSceneGUI]
        private void OnSceneGUI()
        {
            using (new Handles.DrawingScope(transform.localToWorldMatrix))
            {
                if (EditSlices)
                {
                    Handles.color = Color.yellow;
                    Handles.DrawWireCube(_Details.Bounds.center, _Details.Bounds.size);
                    DrawSliceHandles();
                }
                else
                {
                    DrawSizeHandles();
                }
            }
        }

        private void DrawSizeHandles()
        {
            var bounds = ScaledBounds;
            Handles.color = Color.white.WithA(0.5f);
            Handles.DrawWireCube(bounds.center, bounds.size);

            for (var axis = 0; axis < 3; axis++)
            {
                var anchor = _Details.Anchor[axis];
                if (anchor != ScaleAnchor.Min)
                    DrawSizeHandle(axis, -1, bounds);
                if (anchor != ScaleAnchor.Max)
                    DrawSizeHandle(axis, 1, bounds);
            }
        }

        private void DrawSizeHandle(int axis, int side, Bounds bounds)
        {
            var direction = Vector3.zero;
            direction[axis] = side;
            var position = bounds.center + Vector3.Scale(direction, bounds.extents);
            Handles.color = AxisColor(axis);

            EditorGUI.BeginChangeCheck();
            var moved = Handles.Slider(position, direction, HandleUtility.GetHandleSize(position) * 0.08f, Handles.DotHandleCap, 0);
            if (!EditorGUI.EndChangeCheck())
                return;

            var face = moved[axis];
            var size = _Details.Anchor[axis] switch
            {
                ScaleAnchor.Min => face - bounds.min[axis],
                ScaleAnchor.Max => bounds.max[axis] - face,
                _ => 2 * Mathf.Abs(face - bounds.center[axis])
            };

            Undo.RecordObject(this, "Resize Object");
            var newSize = _Details.Size;
            newSize[axis] = size;
            Size = newSize;
            EditorUtility.SetDirty(this);
        }

        private void DrawSliceHandles()
        {
            var bounds = _Details.Bounds;
            DrawPlaneHandle(ref _Details.SliceMin.x, ref _Details.SliceMax.x, -Vector3.right, Vector3.up, Vector3.forward, bounds, AxisColor(0));
            DrawPlaneHandle(ref _Details.SliceMax.x, ref _Details.SliceMin.x, Vector3.right, Vector3.up, Vector3.forward, bounds, AxisColor(0));

            DrawPlaneHandle(ref _Details.SliceMin.y, ref _Details.SliceMax.y, -Vector3.up, Vector3.right, Vector3.forward, bounds, AxisColor(1));
            DrawPlaneHandle(ref _Details.SliceMax.y, ref _Details.SliceMin.y, Vector3.up, Vector3.right, Vector3.forward, bounds, AxisColor(1));

            DrawPlaneHandle(ref _Details.SliceMin.z, ref _Details.SliceMax.z, -Vector3.forward, Vector3.right, Vector3.up, bounds, AxisColor(2));
            DrawPlaneHandle(ref _Details.SliceMax.z, ref _Details.SliceMin.z, Vector3.forward, Vector3.right, Vector3.up, bounds, AxisColor(2));
        }

        private static Color AxisColor(int axis) => axis switch
        {
            0 => Color.red,
            1 => Color.green,
            _ => Color.cyan
        };

        private void DrawPlaneHandle(ref float value, ref float otherValue, Vector3 axis, Vector3 extendA, Vector3 extendB, Bounds bounds, Color color)
        {
            Handles.color = color;
            var absAxis = axis.Abs();
            var offset = bounds.center - axis * Vector3.Dot(bounds.center, axis);
            var planeCenter = offset + absAxis * value;

            EditorGUI.BeginChangeCheck();

            var newPlaneCenter = Handles.Slider(planeCenter, axis, HandleUtility.GetHandleSize(planeCenter) * 0.2f, CapFunction, 0);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this, "Move Slice Handle");
                var newValue = Vector3.Dot(newPlaneCenter - offset, absAxis);

                var min = Vector3.Dot(bounds.min, absAxis);
                var max = Vector3.Dot(bounds.max, absAxis);
                value = Mathf.Clamp(newValue, min, max);

                if (axis.x < 0 || axis.y < 0 || axis.z < 0)
                {
                    if (value > otherValue) otherValue = value;
                }
                else
                {
                    if (value < otherValue) otherValue = value;
                }

                EditorUtility.SetDirty(this);
            }

            var halfA = extendA * Vector3.Dot(bounds.extents, extendA);
            var halfB = extendB * Vector3.Dot(bounds.extents, extendB);
            var verts = new[]
            {
                planeCenter - halfA - halfB,
                planeCenter - halfA + halfB,
                planeCenter + halfA + halfB,
                planeCenter + halfA - halfB
            };
            Handles.DrawSolidRectangleWithOutline(verts, color.WithA(0.04f), color.WithA(0.7f));
            return;

            void CapFunction(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
            {
                var offsetPosition = position + axis * (size * 0.5f);
                Handles.ConeHandleCap(controlID, offsetPosition, rotation, size, eventType);
            }
        }

        private void CustomModuleDrawer(AObjectScalerModule value, GUIContent label, Func<GUIContent, bool> callNextDrawer)
        {
            GUILayout.Label(value.Label, EditorStyles.boldLabel);
            callNextDrawer.Invoke(label);
            var warning = value.GetWarning(this);
            if (!string.IsNullOrEmpty(warning))
                SirenixEditorGUI.WarningMessageBox(warning);
        }
#endif
    }
}
