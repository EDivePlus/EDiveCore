// Author: František Holubec
// Created: 09.10.2026

using System;
using EDIVE.EditorUtils.EditorPriority;
using EDIVE.OdinExtensions.Editor;
using UnityEditor;
using UnityEngine.Video;

namespace EDIVE.OdinExtensions.NativeEditors
{
    [CustomEditor(typeof(VideoPlayer))]
    [EditorPriority]
    [CanEditMultipleObjects]
    public class VideoPlayerOdinEditor : NativeWrapperOdinEditor<VideoPlayer>
    {
        private static readonly Type VIDEO_PLAYER_EDITOR = Type.GetType("UnityEditor.VideoPlayerEditor, UnityEditor.VideoModule");

        protected override Type BaseEditorType => VIDEO_PLAYER_EDITOR;
    }
}
