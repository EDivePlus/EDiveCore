// Author: František Holubec
// Created: 04.11.2021

#if UNITY_EDITOR
using System.IO;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace EDIVE.Utils
{
    [RequireComponent(typeof(Camera))]
    public class RenderToFile : MonoBehaviour
    {
        [FormerlySerializedAs("resultResolution")]
        [SerializeField]
        private Vector2Int _ResultResolution;
        
        [Button]
        public void Render()
        {
            var cam = GetComponent<Camera>();
            var renderTexture = new RenderTexture(_ResultResolution.x, _ResultResolution.y, 24);
            renderTexture.Create();
            var prevTexture = RenderTexture.active;
            RenderTexture.active = renderTexture;
            var camPrevTexture = cam.targetTexture;
            cam.targetTexture = renderTexture;
            cam.Render();
            var texture = new Texture2D(_ResultResolution.x, _ResultResolution.y);
            texture.ReadPixels(new Rect(0, 0, _ResultResolution.x, _ResultResolution.y), 0, 0);
            texture.Apply(true);
            RenderTexture.active = prevTexture;
            cam.targetTexture = camPrevTexture;

            var filePath = EditorUtility.SaveFilePanel("Save file", Application.dataPath, "result.png", "png");
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            File.WriteAllBytes(filePath, texture.EncodeToPNG());
            DestroyImmediate(texture);
            DestroyImmediate(renderTexture);
        }
    }
}
#endif
