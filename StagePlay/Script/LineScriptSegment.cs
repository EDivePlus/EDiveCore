using System;
using EDIVE.Localization;
using UnityEngine;

namespace EDIVE.StagePlay.Script
{
    [Serializable]
    public class LineScriptSegment : AScriptSegment
    {
        [SerializeField]
        private StagePlayCharacterDefinition _Speaker;

        [SerializeField]
        private SafeLocalizedString _LineLocalized;

        public StagePlayCharacterDefinition Speaker => _Speaker;
        public SafeLocalizedString LineLocalized => _LineLocalized;
    }
}