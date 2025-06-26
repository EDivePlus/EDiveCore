#if ADDRESSABLES

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace EDIVE.AppLoading.LoadItems
{
    public class PrefabReferenceLoadItemDefinition : APrefabLoadItemDefinition
    {
        [PropertyOrder(20)]
        [SerializeField]
        private AssetReferenceGameObject _PrefabReference;
        public AssetReferenceGameObject PrefabReference => _PrefabReference;

#if UNITY_EDITOR
        private bool IsReferenceValid => _PrefabReference.editorAsset != null;
#else
        private bool IsReferenceValid => !string.IsNullOrEmpty(_PrefabReference.AssetGUID);
#endif

        public override bool IsValid => _PrefabReference != null && IsReferenceValid;

        protected override async UniTask<GameObject> CreateInstance()
        {
            var instance = await _PrefabReference.InstantiateAsync();
            OnInstantiated(instance);
            return instance;
        }

#if UNITY_EDITOR
        public override GameObject EditorPrefab => _PrefabReference.editorAsset;

        protected override IEnumerable<GameObject> GetAvailableEditorInstances()
        {
            if (IsValid)
                yield return _PrefabReference.editorAsset;
        }

        internal void SetPrefabReference(AssetReferenceGameObject prefabReference)
        {
            _PrefabReference = prefabReference;
            EditorUtility.SetDirty(this);
        }
#endif
    }
}
#endif
