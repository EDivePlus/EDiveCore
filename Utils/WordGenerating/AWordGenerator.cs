// Author: František Holubec
// Created: 16.06.2025

using EDIVE.Extensions.Random;
using UnityEngine;

namespace EDIVE.Utils.WordGenerating
{
    public abstract class AWordGenerator : ScriptableObject
    {
        public string Generate()
        {
            return GenerateInternal(RandomUtility.UNITY_RANDOM);
        }

        public string Generate(IRandom random)
        {
            return GenerateInternal(random);
        }

        public string Generate(int seed)
        {
            var random = new SystemRandom(seed);
            return GenerateInternal(random);
        }

        protected abstract string GenerateInternal(IRandom random);
    }
}
