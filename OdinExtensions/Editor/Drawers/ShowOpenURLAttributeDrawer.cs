using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    [DrawerPriority(DrawerPriorityLevel.WrapperPriority)]
    [AllowGUIEnabledForReadonly]
    public class ShowOpenURLAttributeDrawer : OdinAttributeDrawer<ShowOpenURLAttribute, string>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            EditorGUILayout.BeginHorizontal();
            {
                CallNextDrawer(label);
                GUIHelper.PushGUIEnabled(true);
                var rect = GUILayoutUtility.GetRect(18, 18, SirenixGUIStyles.Button,  GUILayoutOptions.ExpandWidth(false).Width(18));
                if (SirenixEditorGUI.IconButton(rect, FontAwesomeEditorIcons.SquareArrowUpRightSolid, "Open URL"))
                {
                    Application.OpenURL(ValueEntry.SmartValue);
                }
                GUIHelper.PopGUIEnabled();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
