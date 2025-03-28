using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace EDIVE.Core.Versions
{
    public class AppVersionDisplay : MonoBehaviour
    {
        [SerializeField]
        private AppVersionDefinition _VersionDefinition;

        [PropertySpace]
        [SerializeField]
        private TMP_Text _VersionText;

        [SerializeField]
        private bool _OverrideFormat;

        [HideLabel]
        [ShowIf(nameof(_OverrideFormat))]
        [SerializeField]
        private AppVersionFormat _Format;

        private void Awake()
        {
            _VersionText.text = _OverrideFormat ? _VersionDefinition.CurrentVersion.GetFormatedString(_Format) : _VersionDefinition.VersionString;
        }
    }
}
