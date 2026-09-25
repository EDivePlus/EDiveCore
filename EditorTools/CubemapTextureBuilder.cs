using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.EditorTools
{
    public class CubemapTextureBuilder : OdinEditorWindow
    {
        [MenuItem("Tools/Cubemap Builder")]
        public static void OpenWindow()
        {
            GetWindow<CubemapTextureBuilder>();
        }

        private readonly Texture2D[] _textures = new Texture2D[6];
        private readonly string[] _labels =
        {
            "Right", 
            "Left",
            "Top", 
            "Bottom",
            "Front", 
            "Back"
        };

        private readonly TextureFormat[] _hdrFormats =
        {
            TextureFormat.ASTC_HDR_10x10,
            TextureFormat.ASTC_HDR_12x12,
            TextureFormat.ASTC_HDR_4x4,
            TextureFormat.ASTC_HDR_5x5,
            TextureFormat.ASTC_HDR_6x6,
            TextureFormat.ASTC_HDR_8x8,
            TextureFormat.BC6H,
            TextureFormat.RGBAFloat,
            TextureFormat.RGBAHalf
        };

        private readonly Vector2Int[] _placementRects =
        {
            new(2, 1),
            new(0, 1),
            new(1, 2),
            new(1, 0),
            new(1, 1),
            new(3, 1),
        };

        [OnInspectorGUI]
        private void DrawTextureFields()
        {
            for (var i = 0; i < 6; i++)
            {
                _textures[i] = SirenixEditorFields.UnityPreviewObjectField(_labels[i], _textures[i], typeof(Texture2D), false, 50) as Texture2D;
            }
        }

        [Button]
        private void BuildCubemap()
        {
            // Missing Texture
            if (_textures.Any(t => t == null))
            {
                EditorUtility.DisplayDialog("Cubemap Builder Error", "One or more texture is missing.", "Ok");
                return;
            }

            // Get size
            var size = _textures[0].width;

            // Not all of the same size or square
            if (_textures.Any(t => (t.width != size) || (t.height != size)))
            {
                EditorUtility.DisplayDialog("Cubemap Builder Error", "All the textures need to be the same size and square.", "Ok");
                return;
            }

            var isHDR = _hdrFormats.Any(f => f == _textures[0].format);
            var texturePaths = _textures.Select(AssetDatabase.GetAssetPath).ToArray();

            // Should be ok, ask for the file path.
            var path = EditorUtility.SaveFilePanel("Save Cubemap", Path.GetDirectoryName(texturePaths[0]), "Cubemap", isHDR ? "exr" : "png");

            if (string.IsNullOrEmpty(path)) return;

            var dataPath = Application.dataPath.Replace('\\', '/');
            path = path.Replace('\\', '/');
            if (!path.StartsWith(dataPath + "/"))
            {
                EditorUtility.DisplayDialog("Cubemap Builder Error", "Save the cubemap inside the Assets folder.", "Ok");
                return;
            }

            // Save the readable flag to restore it afterwards
            var readableFlags = _textures.Select(t => t.isReadable).ToArray();

            // Get the importer and mark the textures as readable
            var importers = texturePaths.Select(p => AssetImporter.GetAtPath(p) as TextureImporter).ToArray();

            if (importers.Any(i => i == null))
            {
                EditorUtility.DisplayDialog("Cubemap Builder Error", "All textures must be imported texture assets.", "Ok");
                return;
            }

            // Importer changes apply only on SaveAndReimport
            foreach (var importer in importers)
            {
                if (importer.isReadable)
                    continue;
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            // Build the cubemap texture
            var cubeTexture = new Texture2D(size * 4, size * 3, isHDR ? TextureFormat.RGBAFloat : TextureFormat.RGBA32, false);

            for (var i = 0; i < 6; i++)
            {
                cubeTexture.SetPixels(_placementRects[i].x * size, _placementRects[i].y * size, size, size, _textures[i].GetPixels(0));
            }

            cubeTexture.Apply(false);

            // Save the texture to the specified path, and destroy the temporary object
            var bytes = isHDR ? cubeTexture.EncodeToEXR() : cubeTexture.EncodeToPNG();

            File.WriteAllBytes(path, bytes);

            DestroyImmediate(cubeTexture);

            // Reset the read flags, and reimport everything
            for (var i = 0; i < 6; i++)
            {
                if (importers[i].isReadable == readableFlags[i])
                    continue;
                importers[i].isReadable = readableFlags[i];
                importers[i].SaveAndReimport();
            }

            path = "Assets" + path.Substring(dataPath.Length);

            AssetDatabase.ImportAsset(path);

            var cubeImporter = (TextureImporter) AssetImporter.GetAtPath(path);
            cubeImporter.textureShape = TextureImporterShape.TextureCube;
            // LDR keeps source color space, HDR is linear
            cubeImporter.sRGBTexture = !isHDR && importers[0].sRGBTexture;
            cubeImporter.generateCubemap = TextureImporterGenerateCubemap.FullCubemap;
            cubeImporter.SaveAndReimport();
        }
    }
}
