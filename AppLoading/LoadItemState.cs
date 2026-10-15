using EDIVE.NativeUtils;
using EDIVE.OdinExtensions;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
#endif

namespace EDIVE.AppLoading
{
    public enum LoadItemState
    {
        Undefined,
        Pending,
        Loading,
        Completed
    }

    public static class LoadItemStateExtensions
    {
        public static string GetStateRichSprite(this LoadItemState state) => state.GetSymbol().Color(state.GetColor());

        // Material Symbols glyph of the state
        public static string GetSymbol(this LoadItemState state)
        {
            return state switch
            {
                LoadItemState.Undefined => "\ue887",
                LoadItemState.Pending => "\ue8b5",
                LoadItemState.Loading => "\uef64",
                LoadItemState.Completed => "\ue86c",
                _ => "\ue837"
            };
        }

        public static Color GetColor(this LoadItemState state)
        {
            return state switch
            {
                LoadItemState.Undefined => ColorTools.Red,
                LoadItemState.Pending => ColorTools.Aqua,
                LoadItemState.Loading => ColorTools.Yellow,
                LoadItemState.Completed => ColorTools.Lime,
                _ => Color.clear
            };
        }

#if UNITY_EDITOR
        public static EditorIcon GetEditorIcon(this LoadItemState state)
        {
            return state switch
            {
                LoadItemState.Undefined => FontAwesomeEditorIcons.CircleQuestionSolid,
                LoadItemState.Pending => FontAwesomeEditorIcons.ClockSolid,
                LoadItemState.Loading => FontAwesomeEditorIcons.CircleEllipsisSolid,
                LoadItemState.Completed => FontAwesomeEditorIcons.CircleCheckSolid,
                _ => FontAwesomeEditorIcons.CircleDotSolid
            };
        }
#endif
    }
}
