// Author: František Holubec
// Created: 04.07.2025

using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EDIVE.Utils
{
    public class BoxColliderRendererFitter : MonoBehaviour
    {
        [SerializeField]
        private Renderer _Renderer;

        [SerializeField]
        private BoxCollider _BoxCollider;

        [Button]
        public void Refresh()
        {
            var bounds = GetRendererBounds();
            var localCenter = _BoxCollider.transform.InverseTransformPoint(bounds.center);
            var localSize = _BoxCollider.transform.InverseTransformVector(bounds.size);

            _BoxCollider.center = localCenter;
            _BoxCollider.size = localSize;

#if UNITY_EDITOR
            EditorUtility.SetDirty(_BoxCollider);
#endif
        }

        private Bounds GetRendererBounds()
        {
            if (_Renderer is not SkinnedMeshRenderer skinnedMeshRenderer)
                return _Renderer.bounds;

            var bakedMesh = new Mesh();
            skinnedMeshRenderer.BakeMesh(bakedMesh);

            var vertices = bakedMesh.vertices;
            if (vertices.Length == 0)
                return _Renderer.bounds;

            var min = vertices[0];
            var max = vertices[0];

            foreach (var v in vertices)
            {
                min = Vector3.Min(min, v);
                max = Vector3.Max(max, v);
            }

            var center = (min + max) * 0.5f;
            var size = max - min;

            center = skinnedMeshRenderer.transform.TransformPoint(center);
            return new Bounds(center, size);
        }
    }
}
