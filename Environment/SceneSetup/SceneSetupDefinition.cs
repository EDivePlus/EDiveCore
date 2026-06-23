// Author: František Holubec
// Created: 27.08.2025

using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.AssetTranslation;
using EDIVE.Networking.Scenes;
using EDIVE.VisualPresets.Presets;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Environment.SceneSetup
{
    public class SceneSetupDefinition : AUniqueDefinition, ISerializationCallbackReceiver
    {
        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false)]
        private List<SceneReference> _SceneReferences = new();

        [SerializeField]
        [HideInInspector]
        private List<string> _Scenes;

        [SerializeField]
        private VisualPreset _Visual;

        [SerializeField]
        private bool _SetFirstSceneActive = true;

        public IEnumerable<SceneReference> Scenes => _SceneReferences.Where(s => s.IsValid);
        public VisualPreset Visual => _Visual;
        public bool SetFirstSceneActive => _SetFirstSceneActive;

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
#if UNITY_EDITOR
            if (_Scenes == null || _Scenes.Count == 0) return;
            if (_migrationQueued) return;
            _migrationQueued = true;
            UnityEditor.EditorApplication.delayCall += MigrateLegacyScenes;
#endif
        }

#if UNITY_EDITOR
        [NonSerialized]
        private bool _migrationQueued;

        private void MigrateLegacyScenes()
        {
            _migrationQueued = false;
            if (this == null) return;
            if (_Scenes == null || _Scenes.Count == 0) return;

            _SceneReferences ??= new List<SceneReference>();
            foreach (var path in _Scenes)
            {
                if (string.IsNullOrEmpty(path)) continue;
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(path);
                if (asset == null)
                {
                    Debug.LogWarning($"[SceneSetupDefinition] Could not migrate legacy scene path '{path}' on '{name}'", this);
                    continue;
                }
                _SceneReferences.Add(SceneReference.FromSceneAsset(asset));
            }

            _Scenes = null;
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
            Debug.Log($"[SceneSetupDefinition] Migrated legacy scenes on '{name}'", this);
        }
#endif
    }
}
