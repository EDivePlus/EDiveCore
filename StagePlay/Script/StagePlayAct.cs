using System;
using System.Collections.Generic;
using EDIVE.Localization;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.StagePlay.Script
{
    [Serializable]
    public class StagePlayAct
    {
        [SerializeField]
        private SafeLocalizedString _TitleLocalized;

        [SerializeField]
        private int _Number;

        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false)]
        private List<StagePlaySceneDefinition> _Scenes;

        public SafeLocalizedString TitleLocalized => _TitleLocalized;
        public int Number => _Number;
        public List<StagePlaySceneDefinition> Scenes => _Scenes;
    }
}