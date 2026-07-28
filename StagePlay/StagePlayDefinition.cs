// Author: František Holubec
// Created: 23.06.2025

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Cysharp.Threading.Tasks;
using EDIVE.AssetTranslation;

using JetBrains.Annotations;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

#if UNITY_EDITOR && CSV_HELPER
using CsvHelper;
using CsvHelper.Configuration;
#endif

#if PURRNET
using PurrNet.Packing;
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
        private StagePlaySharedData _SharedData;

        [SerializeField]
        [HideReferenceObjectPicker]
        private List<StagePlaySegment> _ScriptSegments = new();

        public TMP_FontAsset Font => _Font;
        public string Name => _Name;
        public StagePlaySharedData SharedData => _SharedData;
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
    
#if PURRNET
    [UsedImplicitly]
    public static class StagePlayDefinitionNetworkExtensions
    {
        public static void Write(this BitPacker packer, StagePlayDefinition value) => packer.CustomWriteTranslatedDefinition(value);
        public static void Read(this BitPacker packer, ref StagePlayDefinition value) => value = packer.CustomReadTranslatedDefinition<StagePlayDefinition>();
    }
#endif
    
}
