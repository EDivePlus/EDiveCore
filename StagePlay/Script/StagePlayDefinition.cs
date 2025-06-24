// Author: František Holubec
// Created: 23.06.2025

using System.Collections.Generic;
using EDIVE.Localization;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.StagePlay.Script
{
    public class StagePlayDefinition : ScriptableObject
    {
        [SerializeField]
        private SafeLocalizedString _TitleLocalized;

        [SerializeField]
        private SafeLocalizedString _Author;

        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false)]
        private List<StagePlayAct> _Acts;

        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false)]
        private List<StagePlayCharacterDefinition> _Characters;

        public SafeLocalizedString TitleLocalized => _TitleLocalized;
        public SafeLocalizedString Author => _Author;
        public List<StagePlayAct> Acts => _Acts;
        public List<StagePlayCharacterDefinition> Characters => _Characters;
    }
}
