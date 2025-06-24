using System.Collections.Generic;
using EDIVE.Localization;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.StagePlay.Script
{
    public class StagePlaySceneDefinition : ScriptableObject
    {
        [SerializeField]
        private SafeLocalizedString _TitleLocalized;

        [SerializeField]
        private int _Number;

        [SerializeReference]
        [HideReferenceObjectPicker]
        [ListDrawerSettings(ShowFoldout = false)]
        private List<AScriptSegment> _ScriptSegments;

        public SafeLocalizedString TitleLocalized => _TitleLocalized;
        public int Number => _Number;
        public List<AScriptSegment> ScriptSegments => _ScriptSegments;
    }
}