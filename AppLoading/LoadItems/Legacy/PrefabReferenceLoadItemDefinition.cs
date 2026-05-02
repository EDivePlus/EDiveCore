#if ADDRESSABLES
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace EDIVE.AppLoading.LoadItems
{
    public class PrefabReferenceLoadItemDefinition : LoadItemDefinition
    {
        [HideInInspector]
        [SerializeField]
        private AssetReferenceGameObject _PrefabReference;
    }
}
#endif
