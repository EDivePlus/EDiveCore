#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

namespace EDIVE.Tweening
{
    public static class DoTweenEditorPreview
    {
        private static double _previewTime;
        private static Action _onPreviewUpdated;

        public static bool IsPreviewing { get; private set; }
        public static List<Tween> CurrentTweens { get; } = new List<Tween>();

        /// <summary>
        ///     Starts the update loop of tween in the editor. Has no effect during playMode.
        /// </summary>
        /// <param name="onPreviewUpdated">Eventual callback to call after every update</param>
        public static void Start(Action onPreviewUpdated = null)
        {
            if (IsPreviewing || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            IsPreviewing = true;
            _onPreviewUpdated = onPreviewUpdated;
            _previewTime = EditorApplication.timeSinceStartup;

            EditorApplication.update -= PreviewUpdate;
            EditorApplication.update += PreviewUpdate;
            
            PrefabStage.prefabStageClosing -= OnPrefabStageClosing;
            PrefabStage.prefabStageClosing += OnPrefabStageClosing; 
        }

        /// <summary>
        ///     Stops the update loop and clears the onPreviewUpdated callback.
        /// </summary>
        /// <param name="resetTweenTargets">If TRUE also resets the tweened objects to their original state</param>
        public static void Stop(bool resetTweenTargets = false)
        {
            IsPreviewing = false;
            EditorApplication.update -= PreviewUpdate;
            _onPreviewUpdated = null;
            if (resetTweenTargets)
            {
                foreach (var tween in CurrentTweens.ToArray())
                {
                    tween?.Rewind();
                }
            }
            CurrentTweens.Clear();
            ValidateTweens();
        }

        [MenuItem("Tools/Stop All Tweens", priority = 200)]
        public static void StopAndClear()
        {
            Stop();
        }

        /// <summary>
        ///     Readies the tween for editor preview by setting its UpdateType to Manual plus eventual extra settings.
        /// </summary>
        /// <param name="tween">The tween to ready</param>
        /// <param name="clearCallbacks">If TRUE (recommended) removes all callbacks (OnComplete/Rewind/etc)</param>
        /// <param name="preventAutoKill">If TRUE prevents the tween from being auto-killed at completion</param>
        /// <param name="andPlay">If TRUE starts playing the tween immediately</param>
        public static void PrepareTweenForPreview(
            Tween tween,
            bool clearCallbacks = false,
            bool preventAutoKill = true,
            bool andPlay = true)
        {
            CurrentTweens.Add(tween);
            tween.SetUpdate(UpdateType.Manual);
            if (preventAutoKill)
                tween.SetAutoKill(false);
            if (clearCallbacks)
                tween.OnComplete(null).OnStart(null).OnPlay(null).OnPause(null).OnUpdate(null)
                    .OnWaypointChange(null).OnStepComplete(null).OnRewind(null).OnKill(null);
            tween.OnComplete(() => RemoveTweenFromPreview(tween));
            if (!andPlay) return;
            tween.Play();
        }

        public static void RemoveTweenFromPreview(Tween tween)
        {
            if (tween == null) return;
            CurrentTweens.Remove(tween);
            tween.SetUpdate(UpdateType.Normal);
            if (CurrentTweens.Count == 0) Stop();
        }

        private static void PreviewUpdate()
        {
            var previewTime = _previewTime;
            _previewTime = EditorApplication.timeSinceStartup;
            var num = _previewTime - previewTime;
            DOTween.ManualUpdate((float) num, (float) num);

            foreach (var graphic in CurrentTweens.GetAllTargets<Graphic>())
                EditorUtility.SetDirty(graphic);

            _onPreviewUpdated?.Invoke();
        }

        private static void ValidateTweens()
        {
            for (var index = CurrentTweens.Count - 1; index > -1; --index)
            {
                if (CurrentTweens[index] == null || !CurrentTweens[index].active)
                    CurrentTweens.RemoveAt(index);
            }
        }

        private static void OnPrefabStageClosing(PrefabStage stage) => Stop(true);
    }
}
#endif
