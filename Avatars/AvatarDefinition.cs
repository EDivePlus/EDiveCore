using EDIVE.AssetTranslation;
using UnityEngine;

namespace EDIVE.Avatars.Scripts
{
    [CreateAssetMenu(menuName = "EDIVE/Definitions/Avatar")]
    public class AvatarDefinition : AUniqueDefinition
    {
        [SerializeField] private GameObject _AvatarPrefab;

        public GameObject AvatarPrefab => _AvatarPrefab;

        public bool IsValid() => _AvatarPrefab != null;
    }
}
