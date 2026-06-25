using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ActionResolvers;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using ActionNamedValue = Sirenix.OdinInspector.Editor.ActionResolvers.NamedValue;

namespace EDIVE.OdinExtensions.Editor.Drawers
{

    [DrawerPriority(0, 100, 0)]
    public sealed class InlineIconButtonAttributeDrawer<T> : OdinAttributeDrawer<InlineIconButtonAttribute, T>
    {
        private ValueResolver<string> _iconNameResolver;
        private ValueResolver<string> _tooltipResolver;
        private ValueResolver<bool> _showIfResolver;
        private ValueResolver<bool> _enableIfResolver;
        private ActionResolver _clickAction;
        
        private const string RECT_NAMED_VALUE = "fieldRect";
        
        private bool _show = true;
        private bool _enable = true;

        protected override void Initialize()
        {
            _iconNameResolver = ValueResolver.GetForString(Property, Attribute.IconName);
            _tooltipResolver = ValueResolver.GetForString(Property, Attribute.Tooltip);
            _showIfResolver = ValueResolver.Get(Property, Attribute.ShowIf, true);
            _enableIfResolver = ValueResolver.Get(Property, Attribute.EnableIf, true);
            _clickAction = ActionResolver.Get(Property, Attribute.Action, new ActionNamedValue(RECT_NAMED_VALUE, typeof(Rect)));
            
            _show = _showIfResolver.GetValue();
        }

        /// <summary>
        /// Draws the property.
        /// </summary>
        protected override void DrawPropertyLayout(GUIContent label)
        {
            ValueResolver.DrawErrors(_iconNameResolver, _tooltipResolver, _showIfResolver, _enableIfResolver);
            ActionResolver.DrawErrors(_clickAction);
            if (_iconNameResolver.HasError || _clickAction.HasError )
            {
                CallNextDrawer(label);
                return;
            }

            if (Event.current.type == EventType.Layout)
            {
                _show = _showIfResolver.GetValue();
                _enable = _enableIfResolver.GetValue();
            }

            var icon = EditorIconsUtility.GetIcon(_iconNameResolver.GetValue(), Attribute.Bundle);
            if (!_show || icon == null)
            {
                CallNextDrawer(label);
                return;
            }

            var fieldRect = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            CallNextDrawer(label);
            EditorGUILayout.EndVertical();
            var tooltip = !_tooltipResolver.HasError ? _tooltipResolver.GetValue() : "";
            if (string.IsNullOrEmpty(tooltip))
                tooltip = ObjectNames.NicifyVariableName(Attribute.Action);
            var rect = GUILayoutUtility.GetRect(18, 18, SirenixGUIStyles.Button,  GUILayoutOptions.ExpandWidth(false).Width(18));

        
            if (Attribute.GUIAlwaysEnabled) GUIHelper.PushGUIEnabled(true);
            else if (!_enable) GUIHelper.PushGUIEnabled(false);
            if (SirenixEditorGUI.IconButton(rect, icon,tooltip))
            {
                Property.RecordForUndo($"Click {Attribute.Action.SplitPascalCase()}");
                _clickAction.Context.NamedValues.Set(RECT_NAMED_VALUE, fieldRect);
                _clickAction.DoActionForAllSelectionIndices();
                Property.MarkSerializationRootDirty();
            }
            if (Attribute.GUIAlwaysEnabled || !_enable) GUIHelper.PopGUIEnabled();
            EditorGUILayout.EndHorizontal();
        }
    }
}
