using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace EDIVE.AppLoading.LoadItems
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
            ApplyTransform(instance);
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
