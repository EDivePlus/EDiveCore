using UnityEngine;

namespace EDIVE.OdinExtensions
{
    public enum EditorIconTextureType
    {
        Raw,
        Inactive,
        Active,
        Highlighted
    }

#if UNITY_EDITOR
    public static class EditorIconTextureTypeExtensions
    {
        public static Texture GetTexture(this Sirenix.Utilities.Editor.EditorIcon icon, EditorIconTextureType type) => type switch
        {
            EditorIconTextureType.Raw => icon.Raw,
            EditorIconTextureType.Inactive => icon.Inactive,
            EditorIconTextureType.Active => icon.Active,
            EditorIconTextureType.Highlighted => icon.Highlighted,
            _ => throw new System.ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
#endif
}
