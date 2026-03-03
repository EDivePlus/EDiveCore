// Author: František Holubec
// Created: 23.06.2025

using System.Collections.Generic;
using System.IO;
using System.Linq;
using EDIVE.AssetTranslation;
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
        private List<APlaySegment> _ScriptSegments = new();

        public string Name => _Name;
        public List<APlaySegment> ScriptSegments => _ScriptSegments;

#if UNITY_EDITOR
        [Button("Import from CSV")]
        private void ImportFromCSV()
        {
            var filePath = UnityEditor.EditorUtility.OpenFilePanel("Select CSV file", "", "csv");
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;

            var lines = File.ReadAllLines(filePath);
            if (lines.Length == 0)
                return;

            _ScriptSegments ??= new List<APlaySegment>();
            _ScriptSegments.Clear();
            
            for (var i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) 
                    continue;
                
                var parts = line.Split(";");
                if (parts.Length < 2) 
                    continue;

                var characters = parts[0].Split(",").Select(s => s.Trim()).ToList();
                var text = parts[1].Trim();

                if (string.IsNullOrEmpty(text)) 
                    continue;

                if (characters.All(string.IsNullOrWhiteSpace))
                    _ScriptSegments.Add(new DirectionPlaySegment(text));
                else
                    _ScriptSegments.Add(new SpeechPlaySegment(characters, text));
            }
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
    
    // Used by Fishet for serialization of AvatarDefinition references.
    [UsedImplicitly] 
    public static class StagePlayDefinitionExtensions
    {
        public static void WriteStagePlayDefinition(this Writer writer, StagePlayDefinition value) => writer.CustomWriteTranslatedDefinition(value);
        public static StagePlayDefinition ReadStagePlayDefinition(this Reader reader) => reader.CustomReadTranslatedDefinition<StagePlayDefinition>();
    }
}
