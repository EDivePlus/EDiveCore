using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    [DrawerPriority(DrawerPriorityLevel.WrapperPriority)]
    public class ReadOnlyListElementsAttributeDrawer : OdinAttributeDrawer<ReadOnlyListElementsAttribute>
    {
        private bool _isListElement;
        
        protected override void Initialize()
        {
            _isListElement = Property.Parent?.ChildResolver is ICollectionResolver;
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            EditorGUI.BeginDisabledGroup(_isListElement);
            CallNextDrawer(label);
            EditorGUI.EndDisabledGroup();
        }
    }
}
