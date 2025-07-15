// Author: František Holubec
// Created: 16.06.2025

using System;
using System.Collections.Generic;
using System.IO;
using EDIVE.Extensions.Random;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EDIVE.Utils.WordGenerating
{
    public class SimpleAdjectiveNameGenerator : AWordGenerator
    {
        [SerializeField]
        private List<string> _Adjectives = new();

        [SerializeField]
        private List<string> _Nouns = new();

        protected override string GenerateInternal(IRandom random)
        {
            var adjective = _Adjectives.Count == 0 ? "Undefined" :_Adjectives.RandomItem(random);
            var noun = _Nouns.Count == 0 ? "Name" : _Nouns.RandomItem(random);
            return $"{adjective} {noun}";
        }

#if UNITY_EDITOR
        [Button]
        private void LoadCSV()
        {
            var path = EditorUtility.OpenFilePanel("Select CSV", "", "csv");
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("No file selected.");
                return;
            }
            
            try
            {
                var lines = File.ReadAllLines(path);
                foreach (var line in lines)
                {
                    var parts = line.Split(',');
                    if (parts.Length >= 2)
                    {
                        var adjective = parts[0].Trim();
                        var noun = parts[1].Trim();
                        if (!string.IsNullOrEmpty(adjective)) 
                            _Adjectives.Add(adjective);
                        
                        if (!string.IsNullOrEmpty(noun)) 
                            _Nouns.Add(noun);
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid line format: {line}");
                    }
                }

                EditorUtility.SetDirty(this);
                Debug.Log("CSV loaded successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
#endif
        
    }
}
