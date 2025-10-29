// Author: František Holubec
// Created: 02.10.2025

using System.Collections.Generic;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace EDIVE.Procedural.MeshScaling
{
    [ExecuteAlways]
    public class MeshScaler : MonoBehaviour
    {
        [HideLabel]
        [InlineProperty]
        [SerializeField]
        private MeshScalerDetails _Details;
        
        [SerializeField]
        private List<MeshComponent> _Components;
        
        private void Update()
        {
            RecalculateSlicedMesh();
        }

        [OnInspectorInit]
        private void OnInspectorInit()
        {
            InitializeMesh();
        }
        
        [Button]
        private void InitializeMesh()
        {
            foreach (var component in _Components)
            {
                component.Initialize();
            }
        }
        
        [Button]
        private void RecalculateSlicedMesh()
        {
            foreach (var component in _Components)
            {
                component.RecalculateSlicedMesh(_Details);
            }
        }
#if UNITY_EDITOR
        [OnSceneGUI]
        private void OnSceneGUI()
        {
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
            DrawPlaneHandle(ref _Details.SliceStart.x, ref _Details.SliceEnd.x, -Vector3.right, Vector3.up, Vector3.forward, _Details.Bounds, Color.red);
            DrawPlaneHandle(ref _Details.SliceEnd.x, ref _Details.SliceStart.x, Vector3.right, Vector3.up, Vector3.forward, _Details.Bounds, Color.red);

            DrawPlaneHandle(ref _Details.SliceStart.y, ref _Details.SliceEnd.y, -Vector3.up, Vector3.right, Vector3.forward, _Details.Bounds, Color.green);
            DrawPlaneHandle(ref _Details.SliceEnd.y, ref _Details.SliceStart.y, Vector3.up, Vector3.right, Vector3.forward, _Details.Bounds, Color.green);

            DrawPlaneHandle(ref _Details.SliceStart.z, ref _Details.SliceEnd.z, -Vector3.forward, Vector3.right, Vector3.up, _Details.Bounds, Color.cyan);
            DrawPlaneHandle(ref _Details.SliceEnd.z, ref _Details.SliceStart.z,Vector3.forward, Vector3.right, Vector3.up, _Details.Bounds, Color.cyan);
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
#endif
    }
}
