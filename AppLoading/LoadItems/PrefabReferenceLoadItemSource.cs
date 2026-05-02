#if ADDRESSABLES
using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace EDIVE.AppLoading.LoadItems
{
    [Serializable]
    public class PrefabReferenceLoadItemSource : APrefabALoadItemSource
    {
        [SerializeField]
        private AssetReferenceGameObject _PrefabReference;
        public AssetReferenceGameObject PrefabReference => _PrefabReference;
        
        public override bool IsValid => _PrefabReference != null &&
#if UNITY_EDITOR
                                        _PrefabReference.editorAsset != null;
#else
            !string.IsNullOrEmpty(_PrefabReference.AssetGUID);
#endif
        
        public PrefabReferenceLoadItemSource(AssetReferenceGameObject prefabReference)
        {
            _PrefabReference = prefabReference;
        }
        
        protected override async UniTask<GameObject> CreateInstance()
        {
            var instance = await _PrefabReference.InstantiateAsync();
            OnInstantiated(instance);
            return instance;
        }

#if UNITY_EDITOR
        protected override bool TryGetEditorPrefab(out GameObject prefab)
        {
            prefab = null;
            if (!IsValid)
                return false;
            
            prefab = _PrefabReference.editorAsset;
            return true;
        }
#endif
    }
}
#endif
