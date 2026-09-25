using System.IO;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.EditorTools.TextureTools
{
    public class PackMetallicSmoothnessWindow : OdinEditorWindow
    {
        [EnhancedPreviewField]
        [SerializeField]
        private Texture2D _MetallicTexture;
        
        [EnhancedPreviewField]
        [SerializeField]
        private Texture2D _SmoothnessTexture;
        
        [Tooltip("Use Roughness instead of Smoothness texture")]
        [SerializeField]
        private bool _InvertSmoothness;
        
        [Button]
        private void GeneratePackedTexture()
        {
            if (!_MetallicTexture || !_SmoothnessTexture)
            {
                EditorUtility.DisplayDialog("Missing Textures", "Please assign both Metallic and Smoothness textures.", "OK");
                return;
            }

            var mTex = GetReadable(_MetallicTexture);
            var rTex = GetReadable(_SmoothnessTexture);

            var width = mTex.width;
            var height = mTex.height;
            var packed = new Texture2D(width, height, TextureFormat.RGBA32, false, true);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var metal = mTex.GetPixel(x, y);
                    // Sizes may differ, sample by uv
                    var smooth = rTex.GetPixelBilinear((x + 0.5f) / width, (y + 0.5f) / height).r;
                    if (_InvertSmoothness) 
                        smooth = 1f - smooth; // convert roughness to smoothness
                    packed.SetPixel(x, y, new Color(metal.r, metal.g, metal.b, smooth));
                }
            }

            packed.Apply();

            var metallicPath = AssetDatabase.GetAssetPath(_MetallicTexture);
            var outputFolder = Path.GetDirectoryName(metallicPath);
            var originalExt = Path.GetExtension(metallicPath).ToLowerInvariant();
            var extensionType = originalExt is ".jpg" or ".jpeg" ? Extension.JPG : Extension.PNG;
            var newExt = extensionType == Extension.JPG ? ".jpg" : ".png";
            var outputPath = Path.Combine(outputFolder, $"{_MetallicTexture.name}+Smooth{newExt}");
            
            var bytes = extensionType == Extension.JPG  ? packed.EncodeToJPG(95) : packed.EncodeToPNG();
            File.WriteAllBytes(outputPath, bytes);
            DestroyImmediate(mTex);
            DestroyImmediate(rTex);
            DestroyImmediate(packed);
            AssetDatabase.Refresh();
        }

        private static Texture2D GetReadable(Texture2D src)
        {
            // Match source space so raw values survive blit
            var readWrite = UnityEngine.Experimental.Rendering.GraphicsFormatUtility.IsSRGBFormat(src.graphicsFormat) ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear;
            var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.Default, readWrite);
            Graphics.Blit(src, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var readable = new Texture2D(src.width, src.height);
            readable.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
            readable.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return readable;
        }
        
        private enum Extension
        {
            PNG,
            JPG
        }

        [MenuItem("Tools/Texture Tools/Pack Metallic + Smoothness")]
        public static void OpenWindow()
        {
            GetWindow<PackMetallicSmoothnessWindow>().Show();
        }
    }
}
