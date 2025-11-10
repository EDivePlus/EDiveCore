// Author: František Holubec
// Created: 10.11.2025

using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

namespace EDIVE.Utils
{
    public class GradientMaterialAssigner : MonoBehaviour
    {
        [SerializeField]
        [OnValueChanged(nameof(GenerateGradientTexture), true)]
        private Gradient _Gradient;
        
        [SerializeField]
        [OnValueChanged(nameof(RefreshMaterial), true)]
        private Renderer _Renderer;
        
        [SerializeField]
        [OnValueChanged(nameof(RefreshMaterial), true)]
        private int _MaterialIndex;
        
        [SerializeField]
        [OnValueChanged(nameof(RefreshMaterial), true)]
        private string _TexturePropertyName = "_Gradient";
        
        [SerializeField]
        [OnValueChanged(nameof(GenerateGradientTexture), true)]
        private int _TextureSize = 64;
        
        [ReadOnly]
        [EnableGUI]
        [EnhancedPreviewField]
        [SerializeField]
        private Texture2D _GradientTexture;

        public Gradient Gradient
        {
            get => _Gradient;
            set
            {
                _Gradient = value;
                GenerateGradientTexture();
            }
        }

        public int Size
        {
            get => _TextureSize;
            set
            {
                _TextureSize = value;
                GenerateGradientTexture();
            }
        }

        private void Awake()
        {
            RefreshMaterial();
        }

        private void RefreshMaterial()
        {
            if (_Renderer == null)
                return;

            var materials = _Renderer.materials;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                materials = _Renderer.sharedMaterials;
#endif
            if (_MaterialIndex < 0 || _MaterialIndex >= materials.Length)
                return;
            
            var material = materials[_MaterialIndex];
            material.SetTexture(_TexturePropertyName, _GradientTexture);
        }

        [Button]
        private void GenerateGradientTexture()
        {
            if (_GradientTexture == null || _GradientTexture.width != _TextureSize)
            {
                if (_GradientTexture != null)
                    DestroyImmediate(_GradientTexture);
                _GradientTexture = new Texture2D(_TextureSize, 1);
            }
            
            for (var x = 0; x < _TextureSize; x++)
            {
                var color = _Gradient.Evaluate( x / (float) _TextureSize);
                _GradientTexture.SetPixel(x, 0, color);
            }
            _GradientTexture.wrapMode = TextureWrapMode.Clamp;
            _GradientTexture.Apply();

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
            RefreshMaterial();
        }

#if UNITY_EDITOR
        [PropertySpace]
        [Button]
        public void SaveGradientAsTexture()
        {
            GenerateGradientTexture();
            var bytes = _GradientTexture.EncodeToPNG();
            
            var filePath = EditorUtility.SaveFilePanelInProject("Save Gradient Texture", "Gradient", "png", "Save gradient texture to project");
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogWarning("Cancelled saving gradient texture.");
                return;
            }
            
            File.WriteAllBytes(filePath, bytes);
            Debug.Log($"Gradient texture saved to: {filePath}");
            
            AssetDatabase.ImportAsset(filePath);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);

            if (texture != null)
                Debug.Log("Gradient texture successfully imported into project.");
            else
                Debug.LogWarning("File saved but texture import failed. Check the file path.");
            
            AssetDatabase.Refresh();
        }
#endif
    }
}
