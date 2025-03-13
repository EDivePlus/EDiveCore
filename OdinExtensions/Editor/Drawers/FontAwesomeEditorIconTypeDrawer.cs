using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    public class FontAwesomeEditorIconTypeDrawer : OdinValueDrawer<FontAwesomeEditorIconType>
    {
        private static List<EditorIconPair> _cachedIcons;
        private static GenericSelector<EditorIconPair> _cachedSelector;

        protected override void DrawPropertyLayout(GUIContent label)
        {
            var selected = new EditorIconPair(ValueEntry.SmartValue,EditorIconsUtility.GetIcon(ValueEntry.SmartValue.ToString()));
            var selectedLabel = GUIHelper.TempContent($" {selected}", selected.Icon.Highlighted);
            GenericSelector<EditorIconPair>.DrawSelectorDropdown(label, selectedLabel, rect =>
            {
                TryRefreshIcons();
                if (_cachedSelector == null)
                {
                    _cachedSelector = new GenericSelector<EditorIconPair>(null, false, x => x.ToString(), _cachedIcons);
                    _cachedSelector.SelectionConfirmed += selection =>
                    {
                        var value = selection.FirstOrDefault();
                        if (value == null) return;
                        ValueEntry.SmartValue = value.IconType;
                        Property.MarkSerializationRootDirty();
                    };
                    foreach (var menuItem in _cachedSelector.SelectionTree.EnumerateTree())
                    {
                        if (menuItem.Value is not EditorIconPair pair) continue;
                        menuItem.Icon = pair.Icon.Highlighted;
                    }
                }
                _cachedSelector.ShowInPopup(rect);
                return _cachedSelector;
            });
        }

        private class EditorIconPair
        {
            public readonly FontAwesomeEditorIconType IconType;
            public readonly EditorIcon Icon;

            public EditorIconPair(FontAwesomeEditorIconType iconType, EditorIcon icon)
            {
                IconType = iconType;
                Icon = icon;
            }

            protected bool Equals(EditorIconPair other)
            {
                return IconType == other.IconType;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((EditorIconPair) obj);
            }

            public override int GetHashCode()
            {
                return (int) IconType;
            }

            public override string ToString()
            {
                return IconType.ToString();
            }
        }

        private static void TryRefreshIcons()
        {
            if (_cachedIcons != null)
                return;

            _cachedIcons = new List<EditorIconPair>();
            var iconProperties = typeof(FontAwesomeEditorIcons).GetProperties(Flags.StaticPublic).OrderBy(p => p.Name);

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            foreach (var iconProperty in iconProperties)
            {
                var returnType = iconProperty.GetReturnType();
                if (!typeof(EditorIcon).IsAssignableFrom(returnType)) continue;

                var editorIcon = (EditorIcon) iconProperty.GetGetMethod().Invoke(null, null);
                if(Enum.TryParse(iconProperty.Name, out FontAwesomeEditorIconType iconType))
                {
                    _cachedIcons.Add(new EditorIconPair(iconType, editorIcon));
                }
            }
        }
    }
}
