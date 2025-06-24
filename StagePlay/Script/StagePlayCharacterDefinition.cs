using EDIVE.Localization;
using UnityEngine;

namespace EDIVE.StagePlay.Script
{
    public class StagePlayCharacterDefinition : ScriptableObject
    {
        [SerializeField]
        private SafeLocalizedString _NameLocalized;
        
        [SerializeField]
        private SafeLocalizedString _DescriptionLocalized;

        public SafeLocalizedString NameLocalized => _NameLocalized;
        public SafeLocalizedString DescriptionLocalized => _DescriptionLocalized;
    }
}