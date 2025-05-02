using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EDIVE.NativeUtils
{
    public static class TextureExtensions
    {
        public static Texture2D GetCopy(this Texture2D original)
        {
            if (original == null)
                return null;

            var copy = new Texture2D(original.width, original.height);
            copy.SetPixels(original.GetPixels());
            copy.Apply();
            return copy;
        }

        public static Texture2D Get2DCopy(this Texture original)
        {
            if (original == null)
                return null;

            var copy = new Texture2D(original.width, original.height);
            copy.SetPixels((original as Texture2D)?.GetPixels());
            copy.Apply();
            return copy;
        }

        public static Texture2D ToTexture2D(this RenderTexture renderTexture, TextureFormat textureFormat = TextureFormat.RGBA32, bool mipChain = false, bool linear = false)
        {
            if (renderTexture == null)
                return null;

            var newTexture = new Texture2D(renderTexture.width, renderTexture.height, textureFormat, mipChain, linear)
            {
                wrapMode = renderTexture.wrapMode,
                filterMode = renderTexture.filterMode
            };
            renderTexture.ToTexture2D(newTexture);
            return newTexture;
        }

        public static void ToTexture2D(this RenderTexture renderTexture, Texture2D outputTexture)
        {
            if (renderTexture == null || outputTexture == null)
                return;

            var oldRenderTexture = RenderTexture.active;
            RenderTexture.active = renderTexture;

            outputTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            outputTexture.Apply();

            RenderTexture.active = oldRenderTexture;
        }

        public static bool IsSubTexture(Sprite sprite)
        {
            if (sprite == null)
            {
                return false;
            }

            var rect = sprite.rect;
            var texture = sprite.texture;
            return texture != null && (sprite.packed || (rect.width != texture.width || rect.height != texture.height));
        }

        public static Color[,] Get2DPixels(this Texture2D texture)
        {
            var result = new Color[texture.width, texture.height];
            for (var x = 0; x < texture.width; x++)
            {
                for (var y = 0; y < texture.height; y++)
                {
                    result[x, y] = texture.GetPixel(x, y);
                }
            }
            return result;
        }

#if UNITY_EDITOR
        public static void SetTextureReadable(this Texture2D texture, bool isReadable)
        {
            if ( null == texture ) return;

            var assetPath = AssetDatabase.GetAssetPath(texture);
            var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (tImporter == null) return;

            tImporter.isReadable = isReadable;
            AssetDatabase.ImportAsset(assetPath);
            AssetDatabase.Refresh();
        }

        public static bool IsTextureReadable(this Texture2D texture)
        {
            if ( null == texture ) return false;

            var assetPath = AssetDatabase.GetAssetPath(texture);
            var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            return tImporter != null && tImporter.isReadable;
        }
#endif
    }
}
