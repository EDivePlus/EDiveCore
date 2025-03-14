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
        public static string GetColoredShortString(this LoadItemState state)
        {
            return state switch
            {
                LoadItemState.Undefined => "U".Blue(),
                LoadItemState.Pending => "P".Red(),
                LoadItemState.Loading => "L".Yellow(),
                LoadItemState.Completed => "C".Lime(),
                _ => "X"
            };
        }

        public static Color GetColor(this LoadItemState state)
        {
            return state switch
            {
                LoadItemState.Undefined => ColorUtils.Aqua,
                LoadItemState.Pending => ColorUtils.Orange,
                LoadItemState.Loading => ColorUtils.Yellow,
                LoadItemState.Completed => ColorUtils.Green,
                _ => Color.clear
            };
        }

#if UNITY_EDITOR
        public static EditorIcon GetEditorIcon(this LoadItemState state)
        {
            return state switch
            {
                LoadItemState.Undefined => FontAwesomeEditorIcons.BlockQuestionSolid,
                LoadItemState.Pending => FontAwesomeEditorIcons.HourglassHalfSolid,
                LoadItemState.Loading => FontAwesomeEditorIcons.LoaderSolid,
                LoadItemState.Completed => FontAwesomeEditorIcons.SquareCheckSolid,
                _ => FontAwesomeEditorIcons.SquareSolid
            };
        }
#endif
    }
}
