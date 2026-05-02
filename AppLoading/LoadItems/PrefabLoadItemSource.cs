using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.AppLoading.LoadItems
{
    [Serializable]
    public class PrefabLoadItemSource : APrefabALoadItemSource
    {
        [SerializeField]
        private GameObject _Prefab;
        public GameObject Prefab => _Prefab;
        
        public override bool IsValid => _Prefab != null;

        public PrefabLoadItemSource(GameObject prefab)
        {
            _Prefab = prefab;
        }

        protected override UniTask<GameObject> CreateInstance()
        {
            var instance = Object.Instantiate(_Prefab);
            OnInstantiated(instance);
            return new UniTask<GameObject>(instance);
        }
        
#if UNITY_EDITOR
        protected override bool TryGetEditorPrefab(out GameObject prefab)
        {
            prefab = _Prefab;
            return true;
        }
#endif
    }
}
