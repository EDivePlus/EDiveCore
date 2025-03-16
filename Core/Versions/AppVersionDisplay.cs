using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace EDIVE.Core.Versions
{
    public class AppVersionDisplay : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _VersionText;

        [BoxGroup("Version Format")]
        [HideLabel]
        [SerializeField]
        private AppVersionFormat _VersionFormat = AppVersionFormat.DEFAULT;
        
        [SerializeField]
        private AppVersionDefinition _VersionDefinition;

        private void Awake()
        {
            if (_VersionText != null)
            {
                _VersionText.text = _VersionDefinition.Version.GetString(_VersionFormat); 
            }
        }
    }
}
