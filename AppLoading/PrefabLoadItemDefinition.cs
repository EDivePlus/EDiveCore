using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EDIVE.AppLoading
{
    public class PrefabLoadItemDefinition : APrefabLoadItemDefinition
    {
        [PropertyOrder(20)]
        [SerializeField]
        private GameObject _Prefab;

        public GameObject Prefab => _Prefab;
        public override bool IsValid => _Prefab != null;

        protected override UniTask<GameObject> CreateInstance()
        {
            var instance = Instantiate(_Prefab);
            return new UniTask<GameObject>(instance);
        }

#if UNITY_EDITOR
        public override GameObject EditorPrefab => Prefab;

        protected override IEnumerable<GameObject> GetAvailableEditorInstances() { yield return _Prefab; }

        internal void SetPrefab(GameObject prefab)
        {
            _Prefab = prefab;
            EditorUtility.SetDirty(this);
        }
#endif
    }
}
