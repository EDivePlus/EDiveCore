using System;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine.SceneManagement;
#endif

namespace EDIVE.DataStructures
{
    [Serializable]
    public struct SceneField : ISerializationCallbackReceiver
    {
#if UNITY_EDITOR
        [SerializeField]
        private SceneAsset _SceneAsset;
#endif

        [HideInInspector]
        [SerializeField]
        private string _Name;

        [HideInInspector]
        [SerializeField]
        private string _Path;

        [HideInInspector]
        [SerializeField]
        private int _BuildIndex;

        public bool IsValid => !string.IsNullOrEmpty(Name);

        public string Name
        {
            get
            {
#if UNITY_EDITOR
                _Name = _SceneAsset != null ? _SceneAsset.name : null;
#endif
                return _Name;
            }
        }

        public string Path
        {
            get
            {
#if UNITY_EDITOR
                _Path = _SceneAsset != null ? AssetDatabase.GetAssetPath(_SceneAsset) : null;
#endif
                return _Path;
            }
        }

        public int BuildIndex
        {
            get
            {
#if UNITY_EDITOR
                _BuildIndex = _SceneAsset != null ? SceneUtility.GetBuildIndexByScenePath(AssetDatabase.GetAssetPath(_SceneAsset)) : -1;
#endif
                return _BuildIndex;
            }
        }

#if UNITY_EDITOR
        public SceneAsset EditorSceneAsset => _SceneAsset;

        public SceneField(SceneAsset sceneAsset) : this()
        {
            _SceneAsset = sceneAsset;
        }
#endif

        public void OnAfterDeserialize() { }
        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (_SceneAsset == null)
            {
                _Name = null;
                _Path = null;
                _BuildIndex = -1;
                return;
            }

            _Path = AssetDatabase.GetAssetPath(_SceneAsset);
            _Name = _SceneAsset.name;
            _BuildIndex = SceneUtility.GetBuildIndexByScenePath(_Path);
#endif
        }
    }

#if UNITY_EDITOR
    public sealed class SceneFieldDrawer : OdinValueDrawer<SceneField>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            var scene = (SceneAsset) SirenixEditorFields.UnityObjectField(label, ValueEntry.SmartValue.EditorSceneAsset, typeof(SceneAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                ValueEntry.SmartValue = new SceneField(scene);
            }
        }
    }
#endif
}
