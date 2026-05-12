// Author: František Holubec
// Created: 23.06.2025

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Cysharp.Threading.Tasks;
using EDIVE.AssetTranslation;

using JetBrains.Annotations;
using PurrNet.Packing;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

#if UNITY_EDITOR && CSV_HELPER
using CsvHelper;
using CsvHelper.Configuration;
#endif

#if FISHNET
using FishNet.Serializing;
#endif

namespace EDIVE.StagePlay
{
    public class StagePlayDefinition : AUniqueDefinition
    {
        [SerializeField]
        private string _Name;
        
        [SerializeField]
        private TMP_FontAsset _Font;

        [SerializeField]
        [HideReferenceObjectPicker]
        private List<StagePlaySegment> _ScriptSegments = new();

        public TMP_FontAsset Font => _Font;
        public string Name => _Name;
        public List<StagePlaySegment> ScriptSegments => _ScriptSegments;

#if UNITY_EDITOR && CSV_HELPER
        [Button]
        private async UniTask ImportFromCSV(int columnOffset)
        {
            columnOffset = Mathf.Max(0, columnOffset);
            // File picker must run on main thread
            var filePath = UnityEditor.EditorUtility.OpenFilePanel("Select CSV file", "", "csv");
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";"
            };
            
            try
            {
                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, config);

                // Read and skip header row
                if (!await csv.ReadAsync() || !csv.ReadHeader())
                {
                    UnityEditor.EditorUtility.DisplayDialog("Import Error", "Could not read header row.", "OK");
                    return;
                }
                
                _ScriptSegments ??= new List<StagePlaySegment>();
                _ScriptSegments.Clear();
                
                while (await csv.ReadAsync())
                {
                    var characters = GetCSVText(csv, columnOffset);
                    var text = GetCSVText(csv, columnOffset + 1);

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

        private string GetCSVText(CsvReader reader, int columnOffset)
        {
            var text = reader.GetField(columnOffset);
            if (text == null)
                return string.Empty;

            text = GetCleanString(text).Trim();
            return text;
        }

        private static string GetCleanString(string text)
        {
            var buffer = new char[text.Length];
            var pos = 0;
            foreach (var c in text)
            {
                if (!char.IsControl(c) || char.IsWhiteSpace(c))
                    buffer[pos++] = c;
            }
            return new string(buffer, 0, pos);
        }
#endif
    }
    
    // Used by PurrNet for serialization of StagePlayDefinition references.
    // PurrNet auto-discovers static classes that contain `Write(this BitPacker, T)` and
    // `Read(this BitPacker, ref T)` extension method pairs — the method names must be
    // exactly "Write"/"Read" and the Read variant must use `ref T`.
    [UsedImplicitly]
    public static class StagePlayDefinitionNetworkExtensions
    {
        public static void Write(this BitPacker packer, StagePlayDefinition value) => packer.CustomWriteTranslatedDefinition(value);
        public static void Read(this BitPacker packer, ref StagePlayDefinition value) => value = packer.CustomReadTranslatedDefinition<StagePlayDefinition>();
    }
    
#if FISHNET
    // Used by FishNet for serialization of StagePlayDefinition references.
    // Renamed from StagePlayDefinitionNetworkExtensions to avoid CS0101 collision
    // with the PurrNet (BitPacker) extension class above while FISHNET is still defined.
    [UsedImplicitly]
    public static class StagePlayDefinitionFishNetExtensions
    {
        public static void WriteStagePlayDefinition(this Writer writer, StagePlayDefinition value) => writer.CustomWriteTranslatedDefinition(value);
        public static StagePlayDefinition ReadStagePlayDefinition(this Reader reader) => reader.CustomReadTranslatedDefinition<StagePlayDefinition>();
    }
#endif
}
