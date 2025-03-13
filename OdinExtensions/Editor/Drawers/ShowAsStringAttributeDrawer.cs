using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    public class ShowAsStringAttributeDrawer<T> : OdinAttributeDrawer<ShowAsStringAttribute, T>, IDefinesGenericMenuItems
    {
        private GUIStyle _labelStyle;
        private GUIStyle _overflowLabelStyle;

        protected override void Initialize()
        {
            base.Initialize();
            _overflowLabelStyle = new GUIStyle(EditorStyles.label)
            {
                richText = Attribute.RichText,
                fontStyle = Attribute.Bold ? FontStyle.Bold : FontStyle.Normal
            };
            _labelStyle = new GUIStyle(SirenixGUIStyles.MultiLineLabel)
            {
                richText = Attribute.RichText,
                fontStyle = Attribute.Bold ? FontStyle.Bold : FontStyle.Normal
            };
        }

        public void PopulateGenericMenu(InspectorProperty property, GenericMenu genericMenu)
        {
            genericMenu.AddItem(new GUIContent("Copy to clipboard"), false, () =>
            {
                GUIUtility.systemCopyBuffer = ValueEntry.SmartValue == null ? "Null" : ValueEntry.SmartValue.ToString();
            });
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            var entry = ValueEntry;
            var attribute = Attribute;

            if (entry.Property.ChildResolver is ICollectionResolver)
            {
                CallNextDrawer(label);
                return;
            }

            var str = entry.SmartValue != null ? entry.SmartValue.ToString() : "Null";
            if (attribute.Nicify)
            {
                str = ObjectNames.NicifyVariableName(str);
            }

            if (label == null)
            {
                EditorGUILayout.LabelField(str, Attribute.Overflow ? _overflowLabelStyle : _labelStyle, GUILayoutOptions.MinWidth(0));
            }
            else if (!attribute.Overflow)
            {
                var stringLabel = GUIHelper.TempContent(str);
                var position = EditorGUILayout.GetControlRect(false, SirenixGUIStyles.MultiLineLabel.CalcHeight(stringLabel, entry.Property.LastDrawnValueRect.width - GUIHelper.BetterLabelWidth),
                    GUILayoutOptions.MinWidth(0));
                var rect = EditorGUI.PrefixLabel(position, label);
                GUI.Label(rect, stringLabel, _labelStyle);
            }
            else
            {
                SirenixEditorGUI.GetFeatureRichControlRect(label, out _, out _, out var rect);
                GUI.Label(rect, str, _overflowLabelStyle);
            }
        }
    }
}
