#if ADDRESSABLES
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace EDIVE.AppLoading.LoadItems
{
    // Frees addressable instance ref on destroy
    [AddComponentMenu("")]
    public class AddressableInstanceReleaser : MonoBehaviour
    {
        private void OnDestroy()
        {
            Addressables.ReleaseInstance(gameObject);
        }
    }
}
#endif
