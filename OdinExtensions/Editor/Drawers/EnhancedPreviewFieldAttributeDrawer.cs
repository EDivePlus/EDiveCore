using System;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    [UsedImplicitly]
    [AllowGUIEnabledForReadonly]
    public sealed class EnhancedPreviewFieldAttributeDrawer<T> : OdinAttributeDrawer<EnhancedPreviewFieldAttribute, T> where T : Object
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            SirenixEditorGUI.BeginHorizontalPropertyLayout(label);
            switch (Attribute.Alignment)
            {
                case PreviewFieldAlignment.Right:
                    CallNextDrawer(null);
                    DrawPreviewField();
                    break;
                
                case PreviewFieldAlignment.Left:
                    DrawPreviewField();
                    CallNextDrawer(null);
                    break;
                
                default: 
                    throw new ArgumentOutOfRangeException();
            }
            SirenixEditorGUI.EndHorizontalPropertyLayout();
        }

        private void DrawPreviewField()
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Width(Attribute.Height));
            EditorGUI.BeginChangeCheck();
            if (Attribute.UseGameObject && ValueEntry.SmartValue is Component component)
            {
                var newObject = SirenixEditorFields.UnityPreviewObjectField(
                    component.gameObject,
                    typeof(GameObject),
                    ValueEntry.Property.GetAttribute<AssetsOnlyAttribute>() == null,
                    Attribute.Height == 0 ? GeneralDrawerConfig.Instance.SquareUnityObjectFieldHeight : Attribute.Height,
                    Sirenix.Utilities.Editor.ObjectFieldAlignment.Center);
                if (newObject is GameObject go && go.TryGetComponent<T>(out var tNewValue))
                {
                    ValueEntry.SmartValue = tNewValue;
                }
            }
            else
            {
                ValueEntry.WeakSmartValue = SirenixEditorFields.UnityPreviewObjectField(
                    ValueEntry.SmartValue,
                    ValueEntry.BaseValueType,
                    ValueEntry.Property.GetAttribute<AssetsOnlyAttribute>() == null,
                    Attribute.Height == 0 ? GeneralDrawerConfig.Instance.SquareUnityObjectFieldHeight : Attribute.Height,
                    Sirenix.Utilities.Editor.ObjectFieldAlignment.Center);
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                ValueEntry.Values.ForceMarkDirty();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
