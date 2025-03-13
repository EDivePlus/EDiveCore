#if UNITY_EDITOR
using System;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor
{
    public static class EditorIconsUtility
    {
        private static readonly Type[] EDITOR_ICONS_CLASSES = new Type[]
        {
            typeof(EditorIcons),
            typeof(CustomEditorIcons),
            typeof(FontAwesomeEditorIcons)
        };

        public static Type GetIconsBundleClass(this EditorIconsBundle editorIconsBundle) => editorIconsBundle switch
        {
            EditorIconsBundle.Odin => typeof(EditorIcons),
            EditorIconsBundle.Custom => typeof(CustomEditorIcons),
            EditorIconsBundle.FontAwesome => typeof(FontAwesomeEditorIcons),
            _ => throw new ArgumentOutOfRangeException(nameof(editorIconsBundle), editorIconsBundle, null)
        };

        public static EditorIcon GetIcon(string iconName, EditorIconsBundle? bundle = null)
        {
            if (bundle.HasValue)
            {
                var property = GetIconsBundleClass(bundle.Value).GetProperty(iconName, Flags.StaticPublic);
                if (property != null)
                    return property.GetValue(null, null) as EditorIcon;
            }
            else
            {
                foreach (var editorIconClass in EDITOR_ICONS_CLASSES)
                {
                    var property = editorIconClass.GetProperty(iconName, Flags.StaticPublic);
                    if (property != null)
                        return property.GetValue(null, null) as EditorIcon;
                }
            }
            return null;
        }
        
        public static Texture2D GetIconTexture(string iconName, EditorIconsBundle? bundle = null)
        {
            if (bundle.HasValue)
            {
                var property = GetIconsBundleClass(bundle.Value).GetProperty(iconName, Flags.StaticPublic);
                if (property == null)
                    return null;

                var propertyValue = property.GetValue(null, null);
                switch (propertyValue)
                {
                    case EditorIcon editorIcon: return editorIcon.Raw;
                    case Texture2D texture2D: return texture2D;
                }
            }
            else
            {
                foreach (var editorIconClass in EDITOR_ICONS_CLASSES)
                {
                    var property = editorIconClass.GetProperty(iconName, Flags.StaticPublic);
                    if (property == null)
                        continue;

                    var propertyValue = property.GetValue(null, null);
                    switch (propertyValue)
                    {
                        case EditorIcon editorIcon: return editorIcon.Raw;
                        case Texture2D texture2D: return texture2D;
                    }

                }
            }
            return null;
        }
    }
}
#endif
