using System;
using EDIVE.Localization;
using UnityEngine;

namespace EDIVE.StagePlay.Script
{
    [Serializable]
    public class DirectionScriptSegment : AScriptSegment
    {
        [SerializeField]
        private SafeLocalizedString _DescriptionLocalized;

        public SafeLocalizedString DescriptionLocalized => _DescriptionLocalized;
    }
}