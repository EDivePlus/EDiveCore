// Author: František Holubec
// Created: 19.10.2021

#if MESH_SIMPLIFIER
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityMeshSimplifier;

#if UNITY_EDITOR
using System.IO;
using UnityEditor.Formats.Fbx.Exporter;
using UnityEditor;
#endif

namespace ProtoGIS.Scripts.Utils
{
    public class MeshSimplifierUtility : MonoBehaviour
    {
        [FormerlySerializedAs("target")]
        [SerializeField]
        private MeshSimplifierTarget _Target;
        
        [FormerlySerializedAs("meshFilter")]
        [SerializeField]
        [ShowIf(nameof(_Target), MeshSimplifierTarget.MeshFilter)]
        private MeshFilter _MeshFilter;
        
        [FormerlySerializedAs("meshCollider")]
        [SerializeField]
        [ShowIf(nameof(_Target), MeshSimplifierTarget.MeshCollider)]
        private MeshCollider _MeshCollider;

        [FormerlySerializedAs("originalMesh")]
        [SerializeField]
        private Mesh _OriginalMesh;
        
        [FormerlySerializedAs("qualityIterations")]
        [SerializeField]
        [LabelWidth(50)]
        [ListDrawerSettings(ShowFoldout = false, ShowIndexLabels = true)]
        private List<QualityIterationSetupElement> _QualityIterations;

        [Serializable]
        [InlineProperty]
        private class QualityIterationSetupElement
        {
            [FormerlySerializedAs("quality")]
            [SerializeField]
            [Range(0f, 1f)]
            [LabelWidth(80)]
            private float _Quality = 0.5f;
            
            [FormerlySerializedAs("simplificationOptions")]
            [SerializeField]
            [HideLabel]
            [LabelWidth(200)]
            [FoldoutGroup("Simplification Options")]
            private SimplificationOptions _SimplificationOptions = SimplificationOptions.Default;
            
            public float Quality => _Quality;
            public SimplificationOptions SimplificationOptions => _SimplificationOptions;
        }

        [PropertySpace]
        [Button]
        public void Simplify()
        {
#if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Simplifying Mesh", $"Simplifying given mesh to {_Target.ToString()}.", 0);
#endif
            var newMesh = _OriginalMesh;
            foreach (var qualitySetup in _QualityIterations)
            {
                var simplifier = new MeshSimplifier(newMesh)
                {
                    SimplificationOptions = qualitySetup.SimplificationOptions
                };
                
                simplifier.SimplifyMesh(qualitySetup.Quality); 
                newMesh = simplifier.ToMesh();
            }

            switch (_Target)
            {
                case MeshSimplifierTarget.MeshFilter:
                    _MeshFilter.sharedMesh = newMesh;
                    break;
                case MeshSimplifierTarget.MeshCollider:
                    _MeshCollider.sharedMesh = newMesh;
                    break;
#if UNITY_EDITOR
                case MeshSimplifierTarget.FbxFile:
                    
                    var path = EditorUtility.SaveFilePanel("Save FBX", Application.dataPath, "Object.fbx", "fbx");
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        var go = new GameObject(Path.GetFileNameWithoutExtension(path));
                        var goMeshFilter = go.AddComponent<MeshFilter>();
                        goMeshFilter.sharedMesh = newMesh;
                        ModelExporter.ExportObject(path, go);
                        DestroyImmediate(go);
                    }
                        
                    break;
#endif
                default:
                    throw new ArgumentOutOfRangeException();
            }
#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif

        }
        private enum MeshSimplifierTarget
        {
            MeshFilter,
            MeshCollider,
#if UNITY_EDITOR
            FbxFile
#endif
        }
    }
}
#endif
