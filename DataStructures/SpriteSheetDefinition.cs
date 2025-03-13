using System.Collections.Generic;
using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EDIVE.DataStructures
{
    public class SpriteSheetDefinition : ScriptableObject
    {
        [SerializeField]
        private Sprite[] _Sprites;
        
        public Sprite[] Sprites => _Sprites;
        public int Length => _Sprites.Length;
        
        public Sprite GetSprite(int index)
        {
            index = index.PositiveModulo(_Sprites.Length); 
            return _Sprites[index];
        }

#if UNITY_EDITOR
        [Button]
        private void ImportFromSpriteSheet(Texture2D texture2D)
        {
            if (texture2D == null) return;

            var texturePath = AssetDatabase.GetAssetPath(texture2D);
            var textureImporter = (TextureImporter) AssetImporter.GetAtPath(texturePath);
            if (textureImporter && textureImporter.spriteImportMode != SpriteImportMode.Multiple)
            {
                Debug.LogWarning($"Texture {texture2D.name} is not set to Multiple sprite mode, cannot import sprites from sprite sheet.");
                return;
            }

            var objects = AssetDatabase.LoadAllAssetsAtPath(texturePath);
            var spriteList = new List<Sprite>(objects.Length);
            foreach (var obj in objects)
            {
                if (obj is Sprite sprite)
                    spriteList.Add(sprite);
            }

            _Sprites = spriteList.ToArray();
        }
#endif
    }
}
