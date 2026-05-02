using EDIVE.AppLoading.Loadables;
using UnityEngine;

namespace EDIVE.AppLoading.LoadItems
{
    public class SerializedLoadItemDefinition : LoadItemDefinition
    {
        [HideInInspector]
        [SerializeReference]
        private ILoadable _Target;
    }
}
