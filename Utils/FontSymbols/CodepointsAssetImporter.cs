// Author: František Holubec
// Created: 10.06.2025

#if UNITY_EDITOR
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace EDIVE.Utils.FontSymbols
{
    [ScriptedImporter(1, "codepoints")]
    public class CodepointsAssetImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext context)
        {
            var asset = ScriptableObject.CreateInstance<CodepointsAsset>();
            var lines = File.ReadAllLines(context.assetPath);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(' ');
                if (parts.Length != 2)
                    continue;

                asset.Entries.Add(new Codepoint(parts[0].Trim(), parts[1].Trim()));
            }

            context.AddObjectToAsset("main", asset);
            context.SetMainObject(asset);
        }
    }
}
#endif
