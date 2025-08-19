// Author: František Holubec
// Created: 23.06.2025

using System.Collections.Generic;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.VisualPresets.Presets;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.StagePlay
{
    public class StagePlayDefinition : ScriptableObject
    {
        [SerializeField]
        private string _ID;

        [SerializeField]
        private VisualPreset _Visual;

        [SerializeReference]
        [ListDrawerSettings(ShowFoldout = false)]
        private List<AScriptSegment> _ScriptSegments;

        [SerializeReference]
        [EnhancedTableList(ShowFoldout = false)]
        private List<StagePlayLanguage> _Languages;

        public string ID => _ID;
        public VisualPreset Visual => _Visual;
        public List<AScriptSegment> ScriptSegments => _ScriptSegments;
    }
}
