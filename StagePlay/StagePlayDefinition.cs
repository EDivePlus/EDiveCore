// Author: František Holubec
// Created: 23.06.2025

using System.Collections.Generic;
using EDIVE.AssetTranslation;
using EDIVE.OdinExtensions.Attributes;
using FishNet.Serializing;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.StagePlay
{
    public class StagePlayDefinition : AUniqueDefinition
    {
        [SerializeField]
        private string _Name;
        
        [SerializeReference]
        [HideReferenceObjectPicker]
        [ListDrawerSettings(ShowFoldout = false)]
        private List<APlaySegment> _ScriptSegments;

        [SerializeReference]
        [EnhancedTableList(ShowFoldout = false)]
        private List<StagePlayLanguage> _Languages;

        public string Name => _Name;
        public List<APlaySegment> ScriptSegments => _ScriptSegments;
    }
    
    // Used by Fishet for serialization of AvatarDefinition references.
    [UsedImplicitly] 
    public static class StagePlayDefinitionExtensions
    {
        public static void WriteStagePlayDefinition(this Writer writer, StagePlayDefinition value) => writer.CustomWriteTranslatedDefinition(value);
        public static StagePlayDefinition ReadStagePlayDefinition(this Reader reader) => reader.CustomReadTranslatedDefinition<StagePlayDefinition>();
    }
}
