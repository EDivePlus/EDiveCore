// Author: Michal Petr
// Created: 04.11.2025

using EDIVE.StateHandling.ToggleStates;
using JetBrains.Annotations;
using Sirenix.Utilities;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace EDIVE.StateHandling.Editor
{
    [UsedImplicitly]
    public class ToggleStateCustomDrawer : OdinValueDrawer<AToggleState>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            CallNextDrawer(label);

            var multiState = ValueEntry.SmartValue;
            if (multiState == null || string.IsNullOrEmpty(multiState.Description)) 
                return;

            GUIHelper.PushGUIEnabled(true);

            var rect = GUILayoutUtility.GetLastRect().HorizontalPadding(0, 40);

            GUI.Label(rect, multiState.Description, SirenixGUIStyles.RightAlignedGreyMiniLabel);

            GUIHelper.PopGUIEnabled();
        }
    }
}
