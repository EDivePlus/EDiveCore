using EDIVE.AssetTranslation;
using UnityEngine;

namespace EDIVE.Avatars
{
    [CreateAssetMenu(menuName = "EDIVE/Definitions/Avatar")]
    public class AvatarDefinition : AUniqueDefinition
    {
        [SerializeField]
        private AvatarController _AvatarPrefab;

        public AvatarController AvatarPrefab => _AvatarPrefab;

        public bool IsValid() => _AvatarPrefab != null;
    }
}
