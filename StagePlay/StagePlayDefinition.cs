// Author: František Holubec
// Created: 23.06.2025

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using Cysharp.Threading.Tasks;
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

        [SerializeField]
        [HideReferenceObjectPicker]
        [ListDrawerSettings(ShowFoldout = false)]
        private List<StagePlaySegment> _ScriptSegments = new();

        public string Name => _Name;
        public List<StagePlaySegment> ScriptSegments => _ScriptSegments;

#if UNITY_EDITOR
        [BoxGroup("Import")]
        [Button("Import")]
        private async UniTask ImportFromCSV(int columnOffset)
        {
            columnOffset = Mathf.Max(0, columnOffset);
            // File picker must run on main thread
            var filePath = UnityEditor.EditorUtility.OpenFilePanel("Select CSV file", "", "csv");
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                Mode = CsvMode.RFC4180
            };
            
            try
            {
                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, config);

                // Read and skip header row
                if (!await csv.ReadAsync() || !csv.ReadHeader())
                {
                    await UniTask.SwitchToMainThread();
                    UnityEditor.EditorUtility.DisplayDialog("Import Error", "Could not read header row.", "OK");
                    return;
                }
                
                _ScriptSegments ??= new List<StagePlaySegment>();
                _ScriptSegments.Clear();
                
                while (await csv.ReadAsync())
                {
                    var characters = (csv.GetField(columnOffset) ?? string.Empty).Trim();
                    var text = (csv.GetField(columnOffset + 1) ?? string.Empty).Trim();

                    if (string.IsNullOrEmpty(text))
                        continue;

                    var type = !string.IsNullOrEmpty(characters) ? StagePlaySegmentType.Speach : StagePlaySegmentType.Direction;
                    _ScriptSegments.Add(new StagePlaySegment(type, text, characters));
                }
            }
            catch (System.Exception ex)
            {
                UnityEditor.EditorUtility.DisplayDialog("Import Error", ex.Message, "OK");
                return;
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
