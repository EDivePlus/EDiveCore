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
        public static string GetStateRichSprite(this LoadItemState state)
        {
            return state switch
            {
                LoadItemState.Undefined => RichText.Sprite("circle_help", ColorTools.Red),
                LoadItemState.Pending => RichText.Sprite("circle_schedule", ColorTools.Aqua),
                LoadItemState.Loading => RichText.Sprite("circle_pending", ColorTools.Yellow),
                LoadItemState.Completed => RichText.Sprite("circle_check", ColorTools.Lime),
                _ => RichText.Sprite("circle_radio", ColorTools.Pink),
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
