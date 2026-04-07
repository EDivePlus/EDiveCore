// Author: František Holubec
// Created: 02.10.2025

using System;
using System.Collections.Generic;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EDIVE.Procedural.MeshScaling
{
    [ExecuteAlways]
    public class MeshSliceScaler : MonoBehaviour
    {
        [PropertyOrder(-1)]
        [ShowInInspector]
        [OnValueChanged("EditorRefresh", true)]
        public Vector3 Size
        {
            get => _Details.TargetScale.MultiplyElementWise(_Details.Bounds.size);
            set => _Details.TargetScale = value.DivideElementWise(_Details.Bounds.size);
        }

        [PropertySpace]
        [HideLabel]
        [InlineProperty]
        [SerializeField]
        [OnValueChanged("EditorRefresh", true)]
        private MeshSliceScaleDetails _Details = new();

        [PropertySpace]
        [SerializeReference]
        [HideReferenceObjectPicker]
        [CustomValueDrawer("CustomComponentDrawer")]
        [OnValueChanged("EditorRefresh", true)]
        private List<AMeshSliceScalerComponent> _Components = new();

        public Vector3 TargetSize
        {
            get => _Details.TargetScale;
            set
            {
                _Details.TargetScale = value;
                Recalculate();
            }
        }

        private void OnEnable()
        {
            RecalculateBounds();
            Recalculate();
        }

        private void RecalculateBounds()
        {
            var hasBounds = false;
            var bounds = new Bounds(Vector3.zero, Vector3.one);
            foreach (var component in _Components)
            {
                if (component == null)
                    continue;
                component.TryCalculateBounds(transform, out var componentBounds);
                if (!hasBounds)
                {
                    bounds = componentBounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(componentBounds);
                }
            }

            _Details.Bounds = bounds;
            _Details.SliceMin = _Details.SliceMin.Clamp(_Details.Bounds.min, _Details.Bounds.max);
            _Details.SliceMax = _Details.SliceMax.Clamp(_Details.SliceMin, _Details.Bounds.max);
        }

        private void Recalculate(bool force = false)
        {
            var changed = false;
            foreach (var component in _Components)
            {
                changed = changed || (component?.Recalculate(_Details, this, transform, force) ?? false);
            }
            foreach (var component in _Components)
            {
                changed = changed || (component?.Postprocess(_Details, this, transform) ?? false);
            }

#if UNITY_EDITOR
            if (changed)
                EditorUtility.SetDirty(this);
#endif
        }

        private void ShowPreviewOriginal()
        {
            foreach (var component in _Components)
            {
                component?.PreviewOriginal(_Details, this, transform);
            }
        }

#if UNITY_EDITOR
        [PropertyOrder(-10)]
        [ShowInInspector]
        [OnValueChanged("EditorRefresh")]
        private bool PreviewSlicing { get; set; }

        private void EditorRefresh()
        {
            RecalculateBounds();
            
            if (PreviewSlicing)
                ShowPreviewOriginal();
            else
                Recalculate();
        }

        private void HideOriginal()
        {
            if (!PreviewSlicing) 
                return;
            
            Recalculate(true);
            PreviewSlicing = false;
        }
        
        [Button]
        private void ForceRecalculate()
        {
            Recalculate(true);
        }

        [OnInspectorInit]
        private void OnInspectorInit()
        {
            PreviewSlicing = false;
            EditorRefresh();
        }
        
        [OnInspectorDispose]
        private void OnInspectorDispose()
        {
            HideOriginal();
        }

        [OnSceneGUI]
        private void OnSceneGUI()
        {
            if (!PreviewSlicing)
                return;
            
            var matrix = transform.localToWorldMatrix;
            using (new Handles.DrawingScope(matrix))
            {
                Handles.color = Color.yellow;
                Handles.DrawWireCube(_Details.Bounds.center, _Details.Bounds.size);
                DrawSliceHandles();
            }
        }

        private void DrawSliceHandles()
        {
            DrawPlaneHandle(ref _Details.SliceMin.x, ref _Details.SliceMax.x, -Vector3.right, Vector3.up, Vector3.forward, _Details.Bounds, Color.red);
            DrawPlaneHandle(ref _Details.SliceMax.x, ref _Details.SliceMin.x, Vector3.right, Vector3.up, Vector3.forward, _Details.Bounds, Color.red);

            DrawPlaneHandle(ref _Details.SliceMin.y, ref _Details.SliceMax.y, -Vector3.up, Vector3.right, Vector3.forward, _Details.Bounds, Color.green);
            DrawPlaneHandle(ref _Details.SliceMax.y, ref _Details.SliceMin.y, Vector3.up, Vector3.right, Vector3.forward, _Details.Bounds, Color.green);

            DrawPlaneHandle(ref _Details.SliceMin.z, ref _Details.SliceMax.z, -Vector3.forward, Vector3.right, Vector3.up, _Details.Bounds, Color.cyan);
            DrawPlaneHandle(ref _Details.SliceMax.z, ref _Details.SliceMin.z, Vector3.forward, Vector3.right, Vector3.up, _Details.Bounds, Color.cyan);
        }

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

                if ((axis.x < 0 || axis.y < 0 || axis.z < 0))
                {
                    if (value > otherValue) otherValue = value;
                }
                else
                {
                    if (value < otherValue) otherValue = value;
                }

                EditorUtility.SetDirty(this);
            }

            var halfA = extendA * (Vector3.Dot(bounds.extents, extendA));
            var halfB = extendB * (Vector3.Dot(bounds.extents, extendB));
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
                var offsetPosition = position + (axis * (size * 0.5f));
                Handles.ConeHandleCap(controlID, offsetPosition, rotation, size, eventType);
            }
        }
        
        private void CustomComponentDrawer(AMeshSliceScalerComponent value, GUIContent label, Func<GUIContent, bool> callNextDrawer)
        {
            GUILayout.Label(value.Label, EditorStyles.boldLabel);
            callNextDrawer.Invoke(label);
        }
#endif
    }
}
