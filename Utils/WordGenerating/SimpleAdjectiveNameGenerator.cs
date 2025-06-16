// Author: František Holubec
// Created: 16.06.2025

using System.Collections.Generic;
using EDIVE.Extensions.Random;
using UnityEngine;

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
    }
}
