using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Core.Versions
{
    public class AppVersionDefinition : ScriptableObject
    {
        [HideLabel]
        [SerializeField]
        private AppVersion _Version;

        public AppVersion Version
        {
            get => _Version;
            set
            {
                _Version = value;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }
        public static implicit operator AppVersion(AppVersionDefinition versionDefinition) => versionDefinition.Version;
    }
}
