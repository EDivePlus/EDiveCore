using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    public class ListItemRangeIndexAttributeDrawer : OdinAttributeDrawer<ListItemRangeIndexAttribute, int>
    {
        private ValueResolver<int> _getterMinValue;
        private ValueResolver<int> _getterMaxValue;

        private int Min => _getterMinValue?.GetValue() ?? Attribute.Min;
        private int Max => _getterMaxValue?.GetValue() ?? Attribute.Max;

        private int? _nextIndex;

        protected override void Initialize()
        {
            var listProperty = Property.ParentValueProperty.Parent;
            if (listProperty == null)
                return;

            if (Attribute.MinGetter != null) _getterMinValue = ValueResolver.Get<int>(Property, Attribute.MinGetter);
            if (Attribute.MaxGetter != null) _getterMaxValue = ValueResolver.Get<int>(Property, Attribute.MaxGetter);

            listProperty.ValueEntry.OnValueChanged += OnPropertyChanged;
            listProperty.ValueEntry.OnChildValueChanged += OnPropertyChanged;
            RefreshIndexes();
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            var newValue = SirenixEditorFields.DelayedIntField((GUIContent) null, ValueEntry.SmartValue, GUILayout.Width(30));
            if (EditorGUI.EndChangeCheck())
            {
                ValueEntry.SmartValue = Mathf.Clamp(newValue, Min, Max);
            }

            if (_nextIndex.HasValue && _nextIndex > ValueEntry.SmartValue)
            {
                GUILayout.Label($"{ValueEntry.SmartValue}-{_nextIndex}");
            }
            else
            {
                GUILayout.Label($"{ValueEntry.SmartValue}");
            }

            EditorGUILayout.EndHorizontal();
        }

        private void OnPropertyChanged(int idx) => RefreshIndexes();

        private void RefreshIndexes()
        {
            var self = Property.ParentValueProperty;
            var listChildren = self.Parent.Children;
            var count = listChildren.Count;
            if (self.Index >= count - 1)
            {
                _nextIndex = Max;
                return;
            }

            var next = listChildren.Get(self.Index + 1);
            var nextIndexProperty = next.FindChild(p => p.GetAttribute<ListItemRangeIndexAttribute>() != null && p.Name == Property.Name, false);
            if (nextIndexProperty == null)
            {
                _nextIndex = Max;
                return;
            }

            _nextIndex = (int) nextIndexProperty.ValueEntry.WeakSmartValue - 1;
        }
    }
}
