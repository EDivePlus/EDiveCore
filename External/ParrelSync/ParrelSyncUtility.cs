// Author: František Holubec
// Created: 14.03.2025

#if PARREL_SYNC
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using EDIVE.Utils.Json;
using Newtonsoft.Json;
using ParrelSync;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace EDIVE.External.ParrelSync
{
    public static class ParrelSyncUtility
    {
        public static readonly JsonSerializerSettings JSON_SERIALIZER_SETTINGS = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented,
            Error = (_, args) =>
            {
                args.ErrorContext.Handled = true;
                Debug.LogException(args.ErrorContext.Error);
            },
        };

        private static List<ProjectCloneRecord> _cloneRecords = new();
        public static List<ProjectCloneRecord> CloneRecords => _cloneRecords ??= RefreshClones();

        public static string SelfArgumentBundlePath => Path.Combine(ClonesManager.GetCurrentProjectPath(), ClonesManager.ArgumentFileName);

        public static JsonFileWatchingEditor<SyncArgumentsBundle> SelfArgumentsBundle
        {
            get => _selfArgumentsBundleEditor ??= new JsonFileWatchingEditor<SyncArgumentsBundle>(SelfArgumentBundlePath, JSON_SERIALIZER_SETTINGS);
            set => _selfArgumentsBundleEditor = value;
        }
        private static JsonFileWatchingEditor<SyncArgumentsBundle> _selfArgumentsBundleEditor;


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
                argumentsBundle.Data.IsMasterPlaying = EditorApplication.isPlayingOrWillChangePlaymode;
                argumentsBundle.SaveData();
            }
        }

        private static void WatchForStateChange()
        {
            var argumentsBundle = SelfArgumentsBundle.Data;
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
            var bundle = SelfArgumentsBundle.Data;
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
                Debug.LogError("Editor process ID not saved");
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
