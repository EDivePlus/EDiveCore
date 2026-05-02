using UnityEngine;

namespace EDIVE.AppLoading.LoadItems
{
    public class PrefabLoadItemDefinition : LoadItemDefinition
    {
        [HideInInspector]
        [SerializeField]
        private GameObject _Prefab;
    }
}
