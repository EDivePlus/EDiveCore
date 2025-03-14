// Author: František Holubec
// Created: 14.03.2025

#if PARREL_SYNC
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using ParrelSync;
using UnityEditor;

namespace EDIVE.External.ParrelSync
{
    public static class ParrelSyncUtility
    {
        private static List<ProjectCloneRecord> _cloneRecords = new();
        public static List<ProjectCloneRecord> CloneRecords => _cloneRecords ??= RefreshClones();

        public static ParrelSyncArgumentsBundle SelfArgumentsBundle
        {
            get => TryGetSelfArgumentsBundle(out var result) ? result : default;
            set => SetSelfArgumentsBundle(value);
        }

        [InitializeOnLoadMethod]
        public static void Initialize()
        {
            if (ClonesManager.IsClone())
            {
                EditorApplication.update -= WatchForStateChange;
                EditorApplication.update += WatchForStateChange;
            }
            else
            {
                EditorApplication.playModeStateChanged -= OnMasterPlayModeStateChanged;
                EditorApplication.playModeStateChanged += OnMasterPlayModeStateChanged;
            }
        }

        private static void OnMasterPlayModeStateChanged(PlayModeStateChange state)
        {
            foreach (var cloneRecord in CloneRecords)
            {
                var argumentsBundle = cloneRecord.ArgumentsBundle;
                argumentsBundle.IsMasterPlaying = EditorApplication.isPlayingOrWillChangePlaymode;
                cloneRecord.ArgumentsBundle = argumentsBundle;
            }
        }

        private static void WatchForStateChange()
        {
            if (SelfArgumentsBundle.IsMasterPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.EnterPlaymode();
            }
            if (!SelfArgumentsBundle.IsMasterPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.ExitPlaymode();
            }
        }

        public static void CreateCloneFromCurrent()
        {
            ClonesManager.CreateCloneFromCurrent();
            RefreshClones();
        }

        public static void DeleteClone(string cloneProjectPath)
        {
            ClonesManager.DeleteClone(cloneProjectPath);
            RefreshClones();
        }

        public static List<ProjectCloneRecord> RefreshClones()
        {
            _cloneRecords.Clear();
            var cloneProjectsPath = ClonesManager.GetCloneProjectsPath();
            foreach (var cloneProjectPath in cloneProjectsPath)
            {
                _cloneRecords.Add(new ProjectCloneRecord(cloneProjectPath));
            }
            return _cloneRecords;
        }

        public static bool TryGetSelfArgumentsBundle(out ParrelSyncArgumentsBundle result)
        {
            result = default;
            return ClonesManager.IsClone() && TryGetArgumentsBundle(ClonesManager.GetCurrentProjectPath(), out result);
        }

        public static void SetSelfArgumentsBundle(ParrelSyncArgumentsBundle argumentsBundle)
        {
            if (!ClonesManager.IsClone()) return;
            SetArgumentsBundle(ClonesManager.GetCurrentProjectPath(), argumentsBundle);
        }

        public static bool TryGetArgumentsBundle(string projectPath, out ParrelSyncArgumentsBundle result)
        {
            result = default;
            var argumentFilePath = Path.Combine(projectPath, ClonesManager.ArgumentFileName);
            if (!File.Exists(argumentFilePath))
                return false;

            var argumentsData = File.ReadAllText(argumentFilePath, System.Text.Encoding.UTF8);
            var success = true;
            var settings = new JsonSerializerSettings
            {
                Error = (sender, args) => { success = false; args.ErrorContext.Handled = true; },
                MissingMemberHandling = MissingMemberHandling.Error
            };
            result = JsonConvert.DeserializeObject<ParrelSyncArgumentsBundle>(argumentsData, settings);
            return success;
        }

        public static void SetArgumentsBundle(string projectPath, ParrelSyncArgumentsBundle argumentsBundle)
        {
            var argumentFilePath = Path.Combine(projectPath, ClonesManager.ArgumentFileName);
            if (!File.Exists(argumentFilePath))
                return;

            var argumentsData = JsonConvert.SerializeObject(argumentsBundle);
            File.WriteAllText(argumentFilePath, argumentsData, System.Text.Encoding.UTF8);
        }
    }
}
#endif
