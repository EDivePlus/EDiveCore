using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ActionResolvers;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{

    [DrawerPriority(0, 100, 0)]
    public sealed class InlineIconButtonAttributeDrawer<T> : OdinAttributeDrawer<InlineIconButtonAttribute, T>
    {
        private ValueResolver<string> _iconNameResolver;
        private ValueResolver<string> _tooltipResolver;
        private ValueResolver<bool> _showIfResolver;
        private ActionResolver _clickAction;
        

        private bool show = true;

        protected override void Initialize()
        {
            _iconNameResolver = ValueResolver.GetForString(Property, Attribute.IconName);
            _tooltipResolver = ValueResolver.GetForString(Property, Attribute.Tooltip);
            _showIfResolver = ValueResolver.Get(Property, Attribute.ShowIf, true);
            _clickAction = ActionResolver.Get(Property, Attribute.Action);
            show = _showIfResolver.GetValue();
        }

        /// <summary>
        /// Draws the property.
        /// </summary>
        protected override void DrawPropertyLayout(GUIContent label)
        {
            ValueResolver.DrawErrors(_iconNameResolver, _tooltipResolver, _showIfResolver);
            ActionResolver.DrawErrors(_clickAction);
            if (_iconNameResolver.HasError || _clickAction.HasError )
            {
                CallNextDrawer(label);
                return;
            }

            if (Event.current.type == EventType.Layout)
            {
                show = _showIfResolver.GetValue();
            }

            var icon = EditorIconsUtility.GetIcon(_iconNameResolver.GetValue(), Attribute.Bundle);
            if (!show || icon == null)
            {
                CallNextDrawer(label);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            CallNextDrawer(label);
            var tooltip = !_tooltipResolver.HasError ? _tooltipResolver.GetValue() : "";
            var rect = GUILayoutUtility.GetRect(18, 18, SirenixGUIStyles.Button,  GUILayoutOptions.ExpandWidth(false).Width(18));

            if (Attribute.GUIAlwaysEnabled) GUIHelper.PushGUIEnabled(true);
            if (SirenixEditorGUI.IconButton(rect, icon,tooltip))
            {
                Property.RecordForUndo($"Click {Attribute.Action.SplitPascalCase()}");
                _clickAction.DoActionForAllSelectionIndices();
                Property.MarkSerializationRootDirty();
            }
            if (Attribute.GUIAlwaysEnabled) GUIHelper.PopGUIEnabled();
            EditorGUILayout.EndHorizontal();
        }
    }
}
