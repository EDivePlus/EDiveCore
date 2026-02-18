// Author: František Holubec
// Created: 23.06.2025

using System.Collections.Generic;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.StagePlay
{
    public class StagePlayDefinition : ScriptableObject
    {
        [SerializeField]
        private string _Name;
        
        [SerializeReference]
        [ListDrawerSettings(ShowFoldout = false)]
        private List<AScriptSegment> _ScriptSegments;

        [SerializeReference]
        [EnhancedTableList(ShowFoldout = false)]
        private List<StagePlayLanguage> _Languages;

        public string Name => _Name;
        public List<AScriptSegment> ScriptSegments => _ScriptSegments;
    }
}
