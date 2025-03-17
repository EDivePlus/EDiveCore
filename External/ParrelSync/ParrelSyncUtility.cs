// Author: František Holubec
// Created: 14.03.2025

#if PARREL_SYNC
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using ParrelSync;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

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
            SaveProcessID();
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
            var argumentsBundle = SelfArgumentsBundle;
            if (argumentsBundle.SyncPlaymode)
            {
                if (argumentsBundle.IsMasterPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.EnterPlaymode();
                    OnClonePlaymodeEntered();
                }
                if (!argumentsBundle.IsMasterPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.ExitPlaymode();
                }
            }
        }

        private static void OnClonePlaymodeEntered()
        {
            var bundle = SelfArgumentsBundle;
            foreach (var action in bundle.Actions)
            {
                action?.OnPlayModeStarted();
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

            var argumentsData = File.ReadAllText(argumentFilePath, Encoding.UTF8);
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
            File.WriteAllText(argumentFilePath, argumentsData, Encoding.UTF8);
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private static void SaveProcessID()
        {
            if (Application.isBatchMode)
                return;

            var pidFilePath = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "Temp", "UnityEditorPID.txt");
            try
            {
                var processId = Process.GetCurrentProcess().Id;
                File.WriteAllText(pidFilePath, processId.ToString());
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public static void FocusUnityEditor(string projectPath)
        {
            if (!ClonesManager.IsCloneProjectRunning(projectPath))
            {
                Debug.LogError("Unity Editor is not running.");
                return;
            }

            var pidFilePath = Path.Combine(projectPath, "Temp", "UnityEditorPID.txt");
            if (!File.Exists(pidFilePath))
            {
                Debug.LogError("Editor process ID not saved.");
                return;
            }

            try
            {
                var pidText = File.ReadAllText(pidFilePath);
                if (!int.TryParse(pidText, out var processId))
                {
                    Debug.LogError("Failed to parse process ID.");
                    return;
                }

                var unityProcess = Process.GetProcessById(processId);
                SetForegroundWindow(unityProcess.MainWindowHandle);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
#endif
