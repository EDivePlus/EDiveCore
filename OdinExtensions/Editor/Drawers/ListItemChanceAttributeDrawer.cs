using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    public class ListItemChanceAttributeDrawer : OdinAttributeDrawer<ListItemChanceAttribute, float>
    {
        private float _chance;
        private float _maxFrequency;

        protected override void Initialize()
        {
            var listProperty = Property.ParentValueProperty.Parent;
            if (listProperty == null)
                return;

            listProperty.ValueEntry.OnValueChanged += OnPropertyChanged;
            listProperty.ValueEntry.OnChildValueChanged += OnPropertyChanged;
            RefreshData();
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            SirenixEditorGUI.BeginHorizontalPropertyLayout(label);
            EditorGUI.BeginChangeCheck();
            var newValue = SirenixEditorFields.DelayedFloatField((GUIContent) null, ValueEntry.SmartValue);
            if (EditorGUI.EndChangeCheck())
            {
                ValueEntry.SmartValue = newValue;
                Property.MarkSerializationRootDirty();
            }

            GUILayout.Label($"{_chance:P1}", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleRight}, GUILayout.Width(45));
            SirenixEditorFields.ProgressBarField(_chance * _maxFrequency, 0f, _maxFrequency);
            SirenixEditorGUI.EndHorizontalPropertyLayout();
        }

        private void OnPropertyChanged(int idx) => RefreshData();

        private void RefreshData()
        {
            var self = Property.ParentValueProperty;
            var listChildren = self.Parent.Children;

            _maxFrequency = 0f;
            var frequencySum = 0f;
            foreach (var child in listChildren)
            {
                var frequencyField = child.FindChild(p => p.GetAttribute<ListItemChanceAttribute>() != null && p.Name == Property.Name, false);
                if (frequencyField == null)
                    continue;

                var childFrequency = (float) frequencyField.ValueEntry.WeakSmartValue;
                frequencySum += childFrequency;
                _maxFrequency = Mathf.Max(_maxFrequency, childFrequency);
            }

            _chance = 1f / frequencySum * ValueEntry.SmartValue;
        }
    }
}
