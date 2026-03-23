#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor
{
    public static class EditorIconsUtility
    {
        private static readonly Type[] EDITOR_ICONS_CLASSES = 
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

        public static EditorIcon GetIcon(string iconName, EditorIconsBundle? bundle = null, EditorIcon fallback = null)
        {
            if (string.IsNullOrEmpty(iconName))
                return fallback;

            var bundlesToSearch = GetBundleTypesToSearch(bundle);
            foreach (var editorIconClass in bundlesToSearch)
            {
                var property = editorIconClass.GetProperty(iconName, Flags.StaticPublic);
                if (property != null && property.GetValue(null, null) is EditorIcon editorIcon)
                    return editorIcon;
            }
            return fallback;
        }

        private static IEnumerable<Type> GetBundleTypesToSearch(EditorIconsBundle? bundle)
        {
            if (bundle.HasValue)
                yield return bundle.Value.GetIconsBundleClass();
            else
                foreach (var editorIconsClass in EDITOR_ICONS_CLASSES)
                    yield return editorIconsClass;
        }
        
        public static Texture2D GetIconTexture(string iconName, EditorIconsBundle? bundle = null, Texture2D fallback = null)
        {
            if (string.IsNullOrEmpty(iconName))
                return fallback;
            
            var bundlesToSearch = GetBundleTypesToSearch(bundle);
            foreach (var editorIconClass in bundlesToSearch)
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
            return fallback;
        }
    }
}
#endif
